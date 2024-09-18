using Opc.Ua;
using Opc.Ua.Server;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;

namespace CustomServer.Services
{
    public class OpcUaServerService
    {
        private readonly StandardServer _server;

        public OpcUaServerService()
        {
            _server = new StandardServer();
        }

        public async Task StartServer()
        {
            var config = new ApplicationConfiguration()
            {
                ApplicationName = "MyOpcUaServer",
                ApplicationType = ApplicationType.Server,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "Certificates/UA_MachineDefault",
                        SubjectName = "MyOpcUaServer"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "Certificates/UA Applications"
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "Certificates/UA Certificate Authorities"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "Certificates/Rejected"
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
                            SecurityMode = MessageSecurityMode.None,
                            SecurityPolicyUri = SecurityPolicies.None
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
                    TraceMasks = 0
                }
            };

            await config.Validate(ApplicationType.Server).ConfigureAwait(false);

            // Ensure the application certificate exists
            if (config.SecurityConfiguration.ApplicationCertificate.Certificate == null)
            {
                var certificate = CreateSelfSignedCertificate(config.ApplicationName);
                // Save the certificate to the store
                var storePath = config.SecurityConfiguration.ApplicationCertificate.StorePath;
                Directory.CreateDirectory(storePath); 
                var store = new X509Store(config.SecurityConfiguration.ApplicationCertificate.StoreType, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
                store.Close();

                config.SecurityConfiguration.ApplicationCertificate.Certificate = certificate;
            }

            // Ensure at least one transport configuration is present
            if (config.TransportConfigurations.Count == 0)
            {
                // Add default transport configuration if necessary
            }

            // Start the server asynchronously
            await Task.Run(() =>
            {
                _server.Start(configuration: config);
            }).ConfigureAwait(false);
        }

        private X509Certificate2 CreateSelfSignedCertificate(string subjectName)
        {
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(
                    $"CN={subjectName}",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));

                // Export and save the certificate to a file if needed
                var certPath = Path.Combine("Certificates", "UA_MachineDefault", $"{subjectName}.pfx");
                File.WriteAllBytes(certPath, certificate.Export(X509ContentType.Pfx));

                return certificate;
            }
        }
    }
}
