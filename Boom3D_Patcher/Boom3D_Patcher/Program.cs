using System;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Boom3D_Patcher
{
    internal static class Program
    {
        private static bool _isPatched = false;

        public static void Main()
        {
            Console.Title = "Boom3D Patcher";

            Console.WriteLine("Enter Boom3D.exe path: ");
            var path = Console.ReadLine()?.Replace("\"", "");
            var module = ModuleDefMD.Load(path);

            if (IsPacked(module))
            {
                Console.WriteLine("Compressor detected, please remove it before patching the file.");
                Console.ReadKey();
                Environment.Exit(0);
            }

            foreach (var type in module.GetTypes())
            {
                // Find Class PersistentSettings
                if (!type.FullName.Contains("PersistentSettings")) continue;
                Console.WriteLine($"Successfully got {type.FullName}");

                foreach (var method in type.Methods)
                {
                    // Find Method get_IsPurchased
                    if (!method.FullName.Contains("get_IsPurchased")) continue;
                    Console.WriteLine($"Successfully got {method.FullName}\nRewriting existing OpCodes...");

                    /*
                     * Clear actual OpCodes and Add return true
                     * ldc.i4.1
                     * ret
                     */
                    method.Body.Instructions.Clear();
                    method.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
                    method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
                    _isPatched = true;
                }
            }

            if (_isPatched)
            {
                Console.WriteLine("Rewriting done, saving file...");
                var fileName = Path.GetFileName(path);

                try
                {
                    module.Write(fileName);
                }
                catch (Exception err)
                {
                    Console.WriteLine($"Failed to save file. ({err.Message})");
                }

                Console.WriteLine($"File saved ({fileName}).");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Method not found, failed to patch.");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Detect if file is using Compressor from ConfuserEx
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        private static bool IsPacked(ModuleDefMD module)
        {
            for (uint rid = 1; rid <= module.Metadata.TablesStream.FileTable.Rows; ++rid)
            {
                module.TablesStream.TryReadFileRow(rid, out var row);
                string name = module.StringsStream.ReadNoNull(row.Name);
                if (name != "koi") continue;
                return true;
            }

            return false;
        }
    }
}
