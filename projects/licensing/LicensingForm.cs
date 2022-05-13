using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;
using System.Xml;

namespace Licensing
{
    public partial class LicensingForm : Form
    {
        public LicensingForm()
        {
            InitializeComponent();
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            int keySize = int.Parse(KeySize.Text);
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(keySize);
            string privateKey = csp.ToXmlString(true);
            string publicKey = csp.ToXmlString(false);

            File.WriteAllText("PrivateKey.xml", privateKey);
            File.WriteAllText("PublicKey.xml", publicKey);
            File.WriteAllText("PublicKeyBase64.txt", Licensing.XmlToBase64(publicKey));

            MessageBox.Show("Key pair generated :\n\nPrivateKey.xml\nPublicKey.xml\nPublicKeyBase64.txt");
        }

        private void GenerateLicenseFileButton_Click(object sender, EventArgs e)
        {
            string name = UserName.Text;
            string email = UserEmail.Text;
            string type = LicenseType.Text;
            DateTime expirationDateTime = ExpirationDateTime.Value;
            
            string uuid = Guid.NewGuid().ToString();

            if (LicenseUuid.Text != "")
                uuid = LicenseUuid.Text;
            else
                LicenseUuid.Text = uuid.ToString();

            XmlDocument xmlLicense = new XmlDocument();

            XmlDeclaration declaration = xmlLicense.CreateXmlDeclaration("1.0", null, null);
            xmlLicense.AppendChild(declaration);

            XmlElement xmlRoot = xmlLicense.CreateElement("License");
            xmlLicense.AppendChild(xmlRoot);

            XmlElement xmlName = xmlLicense.CreateElement("Name");
            xmlName.InnerText = name;
            xmlRoot.AppendChild(xmlName);

            XmlElement xmlEmail = xmlLicense.CreateElement("Email");
            xmlEmail.InnerText = email;
            xmlRoot.AppendChild(xmlEmail);

            XmlElement xmlType = xmlLicense.CreateElement("Type");
            xmlType.InnerText = type;
            xmlRoot.AppendChild(xmlType);

            XmlElement xmlExpiration = xmlLicense.CreateElement("Expiration"); // UTC time formatted to ISO 8601
            xmlExpiration.InnerText = expirationDateTime.ToString(@"yyyy-MM-ddTHH\:mm\:ss.fffffffZ");
            xmlRoot.AppendChild(xmlExpiration);

            XmlElement xmlId = xmlLicense.CreateElement("Id");
            xmlId.InnerText = uuid;
            xmlRoot.AppendChild(xmlId);

            // Generating signature from license information

            RSACryptoServiceProvider Rsa = new RSACryptoServiceProvider();
            Rsa.FromXmlString(File.ReadAllText("PrivateKey.xml"));
            RSAParameters privateKey = Rsa.ExportParameters(true);

            string signature = Licensing.RSAGetSignature(xmlLicense.OuterXml, privateKey);

            XmlElement xmlSignature = xmlLicense.CreateElement("Signature");
            xmlSignature.InnerText = signature;
            xmlRoot.AppendChild(xmlSignature);

            File.WriteAllText(String.Format("License {0}.xml", email), xmlLicense.OuterXml);

            //MessageBox.Show("License generated :\nLicense.xml");
        }
    }
}
