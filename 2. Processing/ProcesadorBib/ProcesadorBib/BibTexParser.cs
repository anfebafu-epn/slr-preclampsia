using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProcesadorBib
{
    public class BibTexParser
    {
        public class BibTexEntry
        {
            public string Type { get; set; }
            public Dictionary<string, string> Fields { get; set; }

            public string Interpretation { get; set; }

            public BibTexEntry()
            {
                Fields = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Método que lee de letra en letra el archivo para parsear Bibtex
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<BibTexEntry> Parse(string filePath)
        {
            var entries = new List<BibTexEntry>();

            var fileContent = File.ReadAllText(filePath);
            fileContent = fileContent.Replace("\n", " ");

            int level = 0;
            int position = 0;
            int refposition = 0;
            while (true)
            {
                BibTexEntry item = new BibTexEntry();
                Random rnd = new Random();

                // busca el registro
                position = fileContent.IndexOf("@", position);
                if (position < 0) break;

                // busca el tipo de referencia
                refposition = fileContent.IndexOf("{", position);
                string RefType = fileContent.Substring(position + 1, refposition - position - 1);
                item.Type = RefType.Trim().ToUpper();
                level++;

                // busca el fin del registro
                position = refposition;
                string currentWord = "";
                string name = "";
                string value = "";
                bool IsName = true;

                while (true)
                {
                    position++;
                    if (position > fileContent.Length) break;

                    string CurrentLetter = fileContent.Substring(position, 1);

                    if (CurrentLetter == "{")
                    {
                        level++;
                    }
                    else if (CurrentLetter == "}")
                    {
                        level--;
                    }
                    else if (level == 1 && CurrentLetter == ",")
                    {
                        if (IsName == true)
                        {
                            name = currentWord;
                        }
                        if (IsName == false)
                        {
                            value = currentWord;
                        }

                        // si es que ya tiene el key antes, genera un valor aleatorio para que pueda ingresar a la lista
                        if (item.Fields.ContainsKey(name.Trim().ToLower()))
                        {
                            
                            item.Fields.Add(name.Trim().ToLower() + "_" + rnd.Next(9999, 99999).ToString(), value.Trim());
                        }
                        else
                        {
                            item.Fields.Add(name.Trim().ToLower(), value.Trim());
                        }


                        currentWord = "";
                        name = "";
                        value = "";
                        IsName = true;
                    }
                    else if (level == 1 && CurrentLetter == "=")
                    {
                        name = currentWord;
                        currentWord = "";

                        // fin de frase, inicio de valor
                        IsName = false;
                    }
                    else
                        currentWord += CurrentLetter;

                    if (level == 0)
                    {
                        if (name != "")
                        {
                            item.Fields.Add(name.Trim().ToLower(), currentWord.Trim());
                        }

                        break;
                    }
                }

                entries.Add(item);
            }

            return entries;
        }
    }
}