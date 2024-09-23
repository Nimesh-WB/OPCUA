using Microsoft.AspNetCore.Hosting.Server;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Threading.Tasks;

namespace CustomServer.Services
{
    public class TestService
    {
        private readonly StandardServer _server;
        private bool _isServerRunning;

        public TestService()
        {
            _server = new StandardServer();
            _isServerRunning = false;
        }

        public async Task StartServer()
        {
            if (_isServerRunning)
            {
                throw new InvalidOperationException("Server is already running.");
            }

            var config = new ApplicationConfiguration
            {
                ApplicationName = "MyOpcUaServer",
                ApplicationType = ApplicationType.Server,
                SecurityConfiguration = new SecurityConfiguration
                {
                    AutoAcceptUntrustedCertificates = true,
                    ApplicationCertificate = new CertificateIdentifier()
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

            // Load the server certificate
            var serverCertFilePath = "C:\\Users\\Nimesh Jethva\\server.crt"; // Update path
            var serverPrivateKeyFilePath = "C:\\Users\\Nimesh Jethva\\server.key"; // Update path

            // Load the server certificate
            /* var serverPfxFilePath = "C:\\Users\\Nimesh Jethva\\server.pfx"; // Update path
             var serverPfxPassword = "wonderbiz"; // If you set a password*/

            if (File.Exists(serverCertFilePath))
            {
                var cert = new X509Certificate2(serverCertFilePath);
                config.SecurityConfiguration.ApplicationCertificate.Certificate = cert;
            }
            else
            {
                throw new FileNotFoundException("Server PFX certificate not found.", serverCertFilePath);
            }



            /*// Load CA certificate for trusted issuer
            var caCertFilePath = "C:\\Users\\Nimesh Jethva\\ca.crt";
            if (File.Exists(caCertFilePath))
            {
                var caCert = new X509Certificate2(caCertFilePath);
                using (var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(caCert);
                }
            }
            else
            {
                throw new FileNotFoundException("CA certificate not found.", caCertFilePath);
            }*/

            try
            {
                // Validate configuration
                await config.Validate(ApplicationType.Server);
                var applicationInstance = new ApplicationInstance(config);

                // Check or create certificate
                await applicationInstance.CheckApplicationInstanceCertificate(false, CertificateFactory.DefaultKeySize);
            }
            catch (Exception ex)
            {
                // Log the exception (you can replace Console with your logging framework)
                Console.WriteLine($"Error during application instance certificate check: {ex.Message}");
                throw; // Rethrow if you want to propagate the error
            }

        }
    }
}
