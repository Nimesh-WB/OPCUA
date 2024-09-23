using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Opc.Ua.Configuration;

namespace CustomServer.Services
{
    public class OpcUaServerService
    {
        private readonly StandardServer _server;
        private bool _isServerRunning;

        public OpcUaServerService()
        {
            _server = new StandardServer();
            _isServerRunning = false;
        }

        public async Task StartServer()
        {
            if (_isServerRunning)
            {
               StopServer();
            }

            // Specify the common folder for certificates
            var certificateFolder = "Certificates/OpcUaCerts";

            var config = new ApplicationConfiguration
            {
                ApplicationName = "MyOpcUaServer",
                ApplicationType = ApplicationType.Server,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = certificateFolder,
                        SubjectName = "MyOpcUaServer"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = certificateFolder
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = certificateFolder
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = certificateFolder
                    },
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ServerConfiguration = new ServerConfiguration
                {
                    BaseAddresses = { "opc.tcp://localhost:4840" },
                    SecurityPolicies = new ServerSecurityPolicyCollection
                    {
                        new ServerSecurityPolicy
                        {
                            SecurityMode = MessageSecurityMode.SignAndEncrypt,
                            SecurityPolicyUri = SecurityPolicies.Basic256Sha256
                        }
                    },
                    UserTokenPolicies = new UserTokenPolicyCollection
                    {
                        new UserTokenPolicy(UserTokenType.Anonymous)
                    }
                },
                TraceConfiguration = new TraceConfiguration
                {
                    OutputFilePath = "Logs/OpcUaServer.log",
                    TraceMasks = Utils.TraceMasks.All
                }
            };

            // Load or create the certificate if it doesn't exist
            var certIdentifier = config.SecurityConfiguration.ApplicationCertificate;
            var certFilePath = Path.Combine(certificateFolder, "MyOpcUaServer.pfx");
            if (File.Exists(certFilePath))
            {
                // Load the existing certificate
                var certBytes = File.ReadAllBytes(certFilePath);
                certIdentifier.Certificate = new X509Certificate2(certBytes, "Wonderbiz@123"); // Load with the correct password
            }
            else
            {
                // Create a new certificate if it doesn't exist
                var certificatePassword = "Wonderbiz@123"; // Set your password here
                var certificate = CreateSelfSignedCertificate(config.ApplicationName, certificatePassword);
                if (certificate != null)
                {
                    Directory.CreateDirectory(certIdentifier.StorePath);
                    using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                    {
                        store.Open(OpenFlags.ReadWrite);
                        store.Add(certificate);
                    }

                    if (certIdentifier.SubjectName == null)
                    {
                        throw new Exception("Failed to load the application certificate after creation.");
                    }
                }
                else
                {
                    throw new Exception("Certificate creation failed.");
                }
            }

            // Validate configuration
            await config.Validate(ApplicationType.Server);
            var applicationInstance = new ApplicationInstance(config);

            // Check or create certificate
            await applicationInstance.CheckApplicationInstanceCertificate(false, CertificateFactory.DefaultKeySize);

            // Start the server
            await applicationInstance.Start(_server);
            _isServerRunning = true;
        }

        private X509Certificate2 CreateSelfSignedCertificate(string subjectName, string password)
        {
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));

                // Store the certificate in a common folder
                var certDirectory = Path.Combine("Certificates", "OpcUaCerts");
                Directory.CreateDirectory(certDirectory);
                var certPath = Path.Combine(certDirectory, $"{subjectName}.pfx");

                // Export the certificate with a password
                var certBytes = certificate.Export(X509ContentType.Pfx, password);
                File.WriteAllBytes(certPath, certBytes);

                return certificate;
            }
        }

        public void StopServer()
        {
            if (_isServerRunning)
            {
                // Stop the OPC UA server
                _server.Stop();
                _isServerRunning = false;
            }
        }
    }
}
