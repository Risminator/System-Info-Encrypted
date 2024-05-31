using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TimpLab3Setup
{
    class Install
    {
        private const string info_file_name = "\\sys.tat";
        private const string reg_path = "Software\\ChernyakovaDronov";
        public static async Task MyInstall(string dir_path, string prog_name)
        {
            var task = MyDownload(dir_path + prog_name);
            InstallTypeReader("tat", dir_path + prog_name);
            WriteSystemInfo(dir_path);
            SignFile(dir_path + info_file_name);
            await task;
            Process.Start(new ProcessStartInfo(dir_path + prog_name) { Arguments = dir_path + info_file_name + " 1", UseShellExecute = false });
            
        }

        private static async Task MyDownload(string path)
        {
            string link = @"https://www.dropbox.com/s/pxugq2ar1ky6kf6/secur.exe?dl=1"; //ссылка
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync(link);

            using (var fs = new FileStream(@path, FileMode.CreateNew))
            {
                await response.Content.CopyToAsync(fs);
            }
        }
        private static void WriteSystemInfo(string dir_path)
        {
            StringBuilder system_info = new StringBuilder();
            // Выведение информации с помощью Environment
            system_info.AppendLine("Операционная система (номер версии): " + Environment.OSVersion);
            system_info.AppendLine("Разрядность процессора: " + Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"));
            system_info.AppendLine("Модель процессора: " + Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"));
            system_info.AppendLine("Путь к системному каталогу: " + Environment.SystemDirectory);
            system_info.AppendLine("Число процессоров: " + Environment.ProcessorCount);
            system_info.AppendLine("Имя пользователя: " + Environment.UserName);
            // Локальные диски
            system_info.AppendLine("Локальные диски: ");
            foreach (DriveInfo dI in DriveInfo.GetDrives())
            {
                system_info.AppendLine(
                      "\t Диск: " + dI.Name +
                      "\n\t Формат диска: " + dI.DriveFormat +
                      "\n\t Размер диска (ГБ): " + (double)dI.TotalSize / 1024 / 1024 / 1024 +
                      "\n\t Доступное свободное место (ГБ): " + (double)dI.AvailableFreeSpace / 1024 / 1024 / 1024);
                system_info.AppendLine();
            }

            // Выведение информации с помощью WMI
            using (ManagementClass manageClass = new ManagementClass("Win32_OperatingSystem"))
            {
                // Получаем все экземпляры класса
                ManagementObjectCollection manageObjects = manageClass.GetInstances();
                // Получаем набор свойств класса
                PropertyDataCollection properties = manageClass.Properties;
                foreach (ManagementObject obj in manageObjects)
                {
                    foreach (PropertyData property in properties)
                    {
                        try
                        {
                            system_info.AppendLine(property.Name + ":  " +
                                            obj.Properties[property.Name.ToString()].Value);
                        }
                        catch { }
                    }
                }
            }

            // Кодирование содержимого
            string system_info_encoded = EncodeString(system_info.ToString());

            // Запись в файл
            File.WriteAllText(dir_path + info_file_name, system_info_encoded);
        }

        // Кодирование строки XOR'ом каждого байта
        private static string EncodeString(string str)
        {
            byte[] str_bytes = Encoding.UTF8.GetBytes(str.ToString());
            for (int i = 0; i < str_bytes.Length; i++) str_bytes[i] ^= 1;
            return Encoding.UTF8.GetString(str_bytes);
        }

        // Подписывает файл личным ключом и записывает подпись в реестр
        private static void SignFile(string file_path)
        {
            // Прочитаем содержимое файла
            string data_file;
            using (StreamReader sr = new StreamReader(file_path))
            {
                data_file = sr.ReadToEnd();
            }

            byte[] data = Encoding.UTF8.GetBytes(data_file);
            SHA256 alg = SHA256.Create();

            byte[] hash = alg.ComputeHash(data);

            RSAParameters exportSharedParameters;
            byte[] signedHash;

            // Сгенерируем подпись
            using (RSA rsa = RSA.Create())
            {
                exportSharedParameters = rsa.ExportParameters(false);

                RSAPKCS1SignatureFormatter rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
                rsaFormatter.SetHashAlgorithm(nameof(SHA256));

                signedHash = rsaFormatter.CreateSignature(hash);
            }

            // Занесём подпись и открытый ключ в реестр
            WriteSignatureToRegistry(signedHash, exportSharedParameters);
        }

        private static void WriteSignatureToRegistry(byte[] signedHash, RSAParameters exportSharedParameters)
        {
            // создание раздела реестра 
            RegistryKey myKey = Registry.CurrentUser;
            RegistryKey signatureFolder = myKey.CreateSubKey(reg_path, true);

            signatureFolder.SetValue("Signature", signedHash);
            signatureFolder.SetValue("Exponent", exportSharedParameters.Exponent);
            signatureFolder.SetValue("Modulus", exportSharedParameters.Modulus);
            signatureFolder.Close();
        }

        private static bool VerifySignature(string check_path)
        {
            using (RSA rsa = RSA.Create())
            {
                // создание раздела реестра 
                RegistryKey myKey = Registry.CurrentUser;
                RegistryKey signatureFolder = myKey.OpenSubKey(reg_path);

                byte[] signature = (byte[])signatureFolder.GetValue("Signature");
                byte[] Exponent = (byte[])signatureFolder.GetValue("Exponent");
                byte[] Modulus = (byte[])signatureFolder.GetValue("Modulus");

                RSAParameters importSharedParameters = new RSAParameters();
                importSharedParameters.Modulus = Modulus;
                importSharedParameters.Exponent = Exponent;
                rsa.ImportParameters(importSharedParameters);

                // чтение данных из файла
                string check_file;
                using (StreamReader sr = new StreamReader(check_path)) check_file = sr.ReadToEnd();

                SHA256 check_alg = SHA256.Create();

                byte[] check_data = Encoding.UTF8.GetBytes(check_file);
                byte[] check_hash = check_alg.ComputeHash(check_data);

                RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                rsaDeformatter.SetHashAlgorithm(nameof(SHA256));

                return rsaDeformatter.VerifySignature(check_hash, signature);
            }
        }

        private static void InstallTypeReader(string file_type, string open_with_path)
        {
            RegistryKey dir1 = Registry.ClassesRoot;
            RegistryKey sub1 = dir1.CreateSubKey('.' + file_type);

            string DATA1 = file_type + "file";
            sub1.SetValue(null, DATA1);
            sub1.Close();

            RegistryKey dir2 = Registry.ClassesRoot;
            RegistryKey sub2 = dir2.CreateSubKey(DATA1 + "\\Shell\\Open\\Command");

            string DATA2 = "\"" + open_with_path + "\" \"%1\"\0";

            sub2.SetValue(null, DATA2, RegistryValueKind.ExpandString);
            sub2.Close();
        }
    }
}
