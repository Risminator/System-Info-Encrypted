using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace secur
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = args[0];
            var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var account = (NTAccount)sid.Translate(typeof(NTAccount));
            //AddFileSecurity(filePath, account.ToString());
            ResetFileSecurity(filePath, account.ToString());
            Environment.Exit(0);
            if (args.Length > 1)
            {
                Environment.Exit(0);
            }

            Console.Write("Hello, User! Entry the signature folder, please: ");
            string regPath = "Software\\" + Console.ReadLine();

            // поиск и проверка подписи
            if (VerifySignature(filePath, regPath, account))
            {
                Console.WriteLine("The signature is valid.");
                RemoveFileSecurity(filePath, account.ToString());
                string fileContents;
                using (StreamReader sr = new StreamReader(filePath)) fileContents = sr.ReadToEnd();
                string unencodedFileContents = EncodeString(fileContents);
                AddFileSecurity(filePath, account.ToString());
                Console.WriteLine(unencodedFileContents);
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("The signature is not valid.");
                Environment.Exit(0);
            }
        }

        static void RemoveFileSecurity(string fileName, string user)
        {
            FileInfo fInfo = new FileInfo(fileName);
            FileSecurity fSecurity = fInfo.GetAccessControl();
            fSecurity.SetAccessRuleProtection(true, true);
            fSecurity.RemoveAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Deny));
            fSecurity.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.Write | FileSystemRights.Delete | FileSystemRights.AppendData | FileSystemRights.DeleteSubdirectoriesAndFiles, AccessControlType.Deny));
            fSecurity.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.ReadAndExecute, AccessControlType.Allow));
            fInfo.SetAccessControl(fSecurity);
        }

        static void AddFileSecurity(string fileName, string user)
        {
            FileInfo fInfo = new FileInfo(fileName);
            FileSecurity fSecurity = fInfo.GetAccessControl();
            fSecurity.SetAccessRuleProtection(true, true);
            //fSecurity.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.WriteAttributes | FileSystemRights.AppendData | FileSystemRights.WriteExtendedAttributes, AccessControlType.Deny));
            fSecurity.RemoveAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Allow));
            fSecurity.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Deny));
            //fSecurity.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.AppendData | FileSystemRights.Synchronize | FileSystemRights.Modify | FileSystemRights.DeleteSubdirectoriesAndFiles | FileSystemRights.Delete, AccessControlType.Deny));
            fInfo.SetAccessControl(fSecurity);
        }

        static void ResetFileSecurity(string fileName, string user)
        {
            FileInfo fInfo = new FileInfo(fileName);
            FileSecurity fSecurity = fInfo.GetAccessControl();
            fSecurity.SetAccessRuleProtection(true, true);
            fSecurity.RemoveAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Allow));
            fSecurity.RemoveAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Deny));
            //fSecurity.RemoveAccessRule(new FileSystemAccessRule(user, FileSystemRights.Read, AccessControlType.Allow));
            //fSecurity.RemoveAccessRule(new FileSystemAccessRule(user, FileSystemRights.ReadAndExecute, AccessControlType.Allow));
            //fSecurity.RemoveAccessRule(new FileSystemAccessRule(user, FileSystemRights.ReadAndExecute, AccessControlType.Deny));
            //fSecurity.RemoveAccessRule(new FileSystemAccessRule(user, FileSystemRights.Read, AccessControlType.Deny));
            //fSecurity.RemoveAccessRule(new FileSystemAccessRule(user, FileSystemRights.Write, AccessControlType.Deny));
            //fSecurity.RemoveAccessRule(new FileSystemAccessRule(user, FileSystemRights.Delete, AccessControlType.Deny));
            fInfo.SetAccessControl(fSecurity);
        }

        private static bool VerifySignature(string checkPath, string regPath, NTAccount account)
        {
            using (RSA rsa = RSA.Create())
            {
                // создание раздела реестра 
                RegistryKey myKey = Registry.CurrentUser;
                RegistryKey signatureFolder = myKey.OpenSubKey(regPath);
                if (signatureFolder == null)
                {
                    Console.Write("Folder doesn't exist");
                    Environment.Exit(0);
                }

                // получение значений из реестра
                byte[] signature = (byte[])signatureFolder.GetValue("Signature");
                byte[] Exponent = (byte[])signatureFolder.GetValue("Exponent");
                byte[] Modulus = (byte[])signatureFolder.GetValue("Modulus");

                signatureFolder.Close();

                // экспорт открытого ключа для подписи
                RSAParameters importSharedParameters = new RSAParameters();
                importSharedParameters.Modulus = Modulus;
                importSharedParameters.Exponent = Exponent;
                rsa.ImportParameters(importSharedParameters);

                // чтение данных из файла
                string checkFile;
                RemoveFileSecurity(checkPath, account.ToString());
                using (StreamReader sr = new StreamReader(checkPath)) checkFile = sr.ReadToEnd();
                AddFileSecurity(checkPath, account.ToString());

                SHA256 checkAlg = SHA256.Create();

                byte[] checkData = Encoding.UTF8.GetBytes(checkFile);
                byte[] checkHash = checkAlg.ComputeHash(checkData);

                RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                rsaDeformatter.SetHashAlgorithm(nameof(SHA256));

                return rsaDeformatter.VerifySignature(checkHash, signature);
            }
        }

        private static string EncodeString(string str)
        {
            byte[] str_bytes = Encoding.UTF8.GetBytes(str.ToString());
            for (int i = 0; i < str_bytes.Length; i++) str_bytes[i] ^= 1;
            return Encoding.UTF8.GetString(str_bytes);
        }
    }
}
