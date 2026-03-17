//using BibtexLibrary;
using imbSCI.BibTex;
using imbSCI.DataComplex.special;
using Newtonsoft.Json;
using SharpToken;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenAI.Chat;

namespace ProcesadorBib
{
    class Program
    {

        static void Main(string[] args)
        {
            // Evita que la computadora se vaya a suspensión mientras esté corriendo el proceso
            // esto es necesario para evitar que se duerma la computadora en tanto demora el trabajo
            // con la interacción con chatGPT.
            SleepHelper.PreventSleep();

            int proceso = 11;
            if (proceso == 1)
            {
                // Procesamiento de archivos BIB.
                var listACM = BibTexParser.Parse(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper SLR Preclamsia 2025\1. Raw Search\ACM\acm.bib");
                var listSnowBalling = BibTexParser.Parse(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper SLR Preclamsia 2025\1. Raw Search\chatGPT Snowballing\chatgpt.bib");
                var listElsevier = BibTexParser.Parse(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper SLR Preclamsia 2025\1. Raw Search\Elsevier\elsevier.bib");
                var listIEEE = BibTexParser.Parse(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper SLR Preclamsia 2025\1. Raw Search\IEEE\ieee.bib");
                var listPubMed = BibTexParser.Parse(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper SLR Preclamsia 2025\1. Raw Search\PubMed\pubmed.bib");
                var listScopus = BibTexParser.Parse(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper SLR Preclamsia 2025\1. Raw Search\Scopus\scopus.bib");
                var listSpringerLink = BibTexParser.Parse(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper SLR Preclamsia 2025\1. Raw Search\Springer\springer.bib");



                // Abro conexión a la base de datos
                using (DOC_TmpEntities db = new DOC_TmpEntities())
                {
                    // Cargo uno por uno los archivos parseados a la base de datos

                    // Carga ACM
                    LoadDatatoSQL(listACM, db, "ACM");

                    // Carga Snowballing chatgpt
                    LoadDatatoSQL(listSnowBalling, db, "Snowballing");

                    // Carga Elsevier
                    LoadDatatoSQL(listElsevier, db, "Elsevier");

                    // Carga IEEE
                    LoadDatatoSQL(listIEEE, db, "IEEE");

                    // Carga PubMed
                    LoadDatatoSQL(listPubMed, db, "PubMed");

                    // Carga Scopus
                    LoadDatatoSQL(listScopus, db, "Scopus");

                    // Carga SpringerLink
                    LoadDatatoSQL(listSpringerLink, db, "Springerlink");

                    // Grabación de todos los registros en la base de datos
                    db.SaveChanges();
                }
            }

            if (proceso == 2)
            {
                // estado 11 = sin DOI
                // estado 12 =  duplicados por doi
                DeteccionDuplicados();
            }

            if (proceso == 3)
            {

                // Enriquecimiento Cross Ref API
                try
                {
                    // Primero descarga los datos para todos los artículos en JSON, sin procesar
                    // En la tabla lo carga en el campo jsondata
                    CrossRef_DownloadArticleInfo();

                    // Cuando ya está grabado en la base de datos, procesa todos los JSON para extraer la información
                    CrossRef_ProcessArticleInfo();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    Console.WriteLine(ex.EntityValidationErrors.First().ValidationErrors.First().ErrorMessage);
                }

            }

            if (proceso == 4)
            {
                // identificar con chatGPT si es que está relacionado con el tema de investigación
                // criterio de exclusión primer nivel
                chatGPT_Exclusion_Run();
            }

            if (proceso == 5)
            {
                // procesa y genera criterio de exclusión automático de los archivos
                chatGPT_Exclusion_Process();
            }

            if (proceso == 6)
            {
                // extrae los algoritmos o métodos de inteligencia artificial que se han utilizado
                // esta información servirá para responder la primera pregunta de investigación
                chatGPT_RQ1_Run();
            }

            if (proceso == 7)
            {
                // extrae los indicadores de eficiencia de los algoritmos
                chatGPT_RQ2_Run();
            }

            if (proceso == 8)
            {
                // extrae las técnicas de evaluación de eficiencia
                chatGPT_RQ3_Run();
            }


            if (proceso == 9)
            {
                // Búsqueda de fuentes de bases de datos 
                chatGPT_RQ4_Run();
            }

            if (proceso == 10)
            {
                // Método de clasificación de artículos según tags
                chatGPT_Classification_Run();
            }

            if (proceso == 11)
            {
                chatGPT_Process_Run();
            }
        }


        private static void DeteccionDuplicados()
        {
            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                db.BibEntry_Duplicados();
            }
        }

        private static void LoadDatatoSQL(List<BibTexParser.BibTexEntry> dbentries, DOC_TmpEntities context, string db)
        {
            foreach (var item in dbentries)
            {
                BibEntry ne = new BibEntry();
                ne.EntryID = Guid.NewGuid();
                ne.EntryType = item.Type;
                ne.EntryStatus = 1;
                ne.EntryKey = item.Fields.First().Key; // siempre el key del primer registro es el entrykey
                ne.bibtex = ""; // Contenido completo del bibtex. Por ahora no utilizado
                ne.db = db;

                // No toma el primero para llenar dado que contiene el key. Los demás son valores para llenado
                foreach (var tag in item.Fields.Skip(1))
                {
                    // Obtiene en función del key, el campo de la base de datos
                    var field = typeof(BibEntry).GetProperty(tag.Key);

                    // si está null, por si acaso, lo obtiene en función del valor
                    if (field == null)
                        field = typeof(BibEntry).GetProperty(tag.Value);

                    // si sigue null, es que no encuentra el campo en la tabla
                    if (field == null)
                    {
                        // no hay un campo, por lo tanto no se almacena
                        // pero se deja un registro de los campos que no existen
                        System.IO.File.AppendAllText(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper SLR Preclamsia\2. Processing\Log\log.txt", tag.Key + " no existe en la base de datos" + Environment.NewLine);
                    }
                    else
                    {
                        field.SetValue(ne, tag.Value);
                    }
                }

                // Agrega los datos al contexto, pero no los graba todavía
                context.BibEntry.Add(ne);
            }
        }

        private static void CrossRef_DownloadArticleInfo()
        {
            // Método para descargar JSON de Crossref enviando doi
            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                foreach (var ent in db.BibEntry.Where(b => b.jsondata == null && b.EntryStatus == 1).ToList())
                {
                    using (System.Net.WebClient client = new System.Net.WebClient())
                    {
                        try
                        {
                            string data = client.DownloadString("https://api.crossref.org/works/" + ent.doi);
                            ent.jsondata = data;
                        }
                        catch (System.Net.WebException wex)
                        {
                            if (wex.Status != System.Net.WebExceptionStatus.Success)
                            {
                                ent.jsondata = "error";
                                ent.EntryStatus = 10;
                            }
                        }
                    }
                    db.SaveChanges();
                }
            }
        }

        private static void CrossRef_ProcessArticleInfo()
        {
            int counter = 0;
            // Método para procesar la información JSON que vino desde CrossRef Api
            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                var vta = db.BibEntry_Doi_VTA.ToList();

                // itero todos los registros para procesar el json y llenar las otras tablas que se necesitan
                foreach (var ent in db.BibEntry.Where(b => b.EntryStatus == 1 && b.isreferencedbycount.HasValue == false).ToList())
                {
                    Console.WriteLine(ent.EntryID.ToString());
                    if (ent.jsondata == "error") continue;

                    CrossRefStructure.CrossRefStructure str = Newtonsoft.Json.JsonConvert.DeserializeObject<CrossRefStructure.CrossRefStructure>(ent.jsondata);

                    if (str.message.author == null)
                    {
                        // limpieza de artículos que no tienen autor
                        // normalmente porque son carátulas de los proceedings
                        //ent.EntryStatus = 15; // no tiene autor definido
                        //db.SaveChanges();
                        continue;
                    }

                    // grabación de los autores
                    foreach (var a in str.message.author)
                    {
                        Guid AuID = Guid.Empty;
                        // busca si hay un author que ya existe
                        var existingAuthor = db.Author.FirstOrDefault(au => au.given == a.given && au.family == a.family);

                        // si no existe, crea uno
                        if (existingAuthor == null)
                        {
                            Author author = new Author();
                            author.AuthorID = Guid.NewGuid();
                            author.given = a.given;
                            author.family = a.family;

                            db.Author.Add(author);

                            AuID = author.AuthorID;
                        }
                        else
                        {
                            AuID = existingAuthor.AuthorID;
                        }

                        BibEntryAuthor relation = new BibEntryAuthor();
                        relation.BibEntryAuthorID = Guid.NewGuid();
                        relation.AuthorID = AuID;
                        relation.EntryID = ent.EntryID;
                        relation.sequence = a.sequence;

                        db.BibEntryAuthor.Add(relation);
                    }

                    // Grabacion de los eventos
                    if (str.message.cevent != null)
                    {
                        var EVID = Guid.Empty;
                        var existingEvent = db.Event.FirstOrDefault(e => e.Name == str.message.cevent.name);

                        // Si el evento no existe
                        if (existingEvent == null)
                        {
                            Event ev = new Event();
                            ev.EventID = Guid.NewGuid();
                            ev.Name = str.message.cevent.name;
                            ev.Location = str.message.cevent.location;
                            ev.Acronym = str.message.cevent.acronym;

                            db.Event.Add(ev);
                            EVID = ev.EventID;
                        }
                        else
                        {
                            EVID = existingEvent.EventID;
                        }

                        BibEntryEvent bee = new BibEntryEvent();
                        bee.BibEntryEventID = Guid.NewGuid();
                        bee.EntryID = ent.EntryID;
                        bee.EventID = EVID;

                        db.BibEntryEvent.Add(bee);
                    }

                    // referencias
                    if (str.message.reference != null)
                    {
                        var lst = vta.Where(b => str.message.reference.Count(r => r.DOI == b.doi) > 0).ToList();

                        foreach (var item in lst)
                        {
                            BibEntryReference br = new BibEntryReference();
                            br.BibEntryReferenceID = Guid.NewGuid();
                            br.EntryID1 = ent.EntryID;
                            br.EntryID2 = item.EntryID;

                            db.BibEntryReference.Add(br);
                        }

                        //foreach (var aref in str.message.reference)
                        //{
                        //    if (aref.DOI != null)
                        //    {
                        //        var foundArticle = db.BibEntry_Doi_VTA.FirstOrDefault(b => b.doi == aref.DOI);
                        //        if (foundArticle != null)
                        //        {
                        //            BibEntryReference br = new BibEntryReference();
                        //            br.BibEntryReferenceID = Guid.NewGuid();
                        //            br.EntryID1 = ent.EntryID;
                        //            br.EntryID2 = foundArticle.EntryID;

                        //            db.BibEntryReference.Add(br);
                        //        }
                        //    }
                        //}
                    }

                    // subjects
                    if (str.message.subject != null)
                    {
                        foreach (var sub in str.message.subject)
                        {
                            var Sid = Guid.Empty;
                            var existingSubject = db.Subject.FirstOrDefault(s => s.SubjectName == sub);
                            if (existingSubject == null)
                            {
                                Subject s = new Subject();
                                s.SubjectID = Guid.NewGuid();
                                s.SubjectName = sub;

                                db.Subject.Add(s);
                                Sid = s.SubjectID;
                            }
                            else
                            {
                                Sid = existingSubject.SubjectID;
                            }

                            BibEntrySubject bs = new BibEntrySubject();
                            bs.BibEntrySubjectID = Guid.NewGuid();
                            bs.EntryID = ent.EntryID;
                            bs.SubjectID = Sid;

                            db.BibEntrySubject.Add(bs);
                        }
                    }

                    // actualización de datos
                    if (str.message.indexed != null && str.message.indexed.dateparts[0].Count() == 3)
                        ent.indexed = new DateTime(str.message.indexed.dateparts[0][0], str.message.indexed.dateparts[0][1], str.message.indexed.dateparts[0][2]);

                    if (str.message.publishedprint != null && str.message.publishedprint.dateparts[0].Count() == 3)
                        ent.publishedprint = new DateTime(str.message.publishedprint.dateparts[0][0], str.message.publishedprint.dateparts[0][1], str.message.publishedprint.dateparts[0][2]);

                    if (str.message.created != null && str.message.created.dateparts[0].Count() == 3)
                        ent.created = new DateTime(str.message.created.dateparts[0][0], str.message.created.dateparts[0][1], str.message.created.dateparts[0][2]);

                    if (str.message.issued != null && str.message.issued.dateparts[0].Count() == 3)
                        ent.issued = new DateTime(str.message.issued.dateparts[0][0], str.message.issued.dateparts[0][1], str.message.issued.dateparts[0][2]);

                    if (str.message.published != null && str.message.published.dateparts[0].Count() == 3)
                        ent.published = new DateTime(str.message.published.dateparts[0][0], str.message.published.dateparts[0][1], str.message.published.dateparts[0][2]);

                    ent.articletype = str.message.type;
                    ent.score = str.message.score;
                    ent.isreferencedbycount = str.message.isreferencedbycount;
                    if (str.message.ISSN != null)
                        ent.cISSN = str.message.ISSN.Aggregate((a, b) => a + "," + b);
                    if (str.message.ISBN != null)
                        ent.cISBN = str.message.ISBN.Aggregate((a, b) => a + "," + b);

                    if (str.message.containertitle != null && str.message.containertitle.Count > 0)
                        ent.containertitle = str.message.containertitle.Aggregate((a, b) => a + ' ' + b);


                    db.SaveChanges();
                    counter++;
                    Console.WriteLine(counter.ToString());
                }


            }
        }

        private static void chatGPT_Exclusion_Run()
        {
            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                Random rnd = new Random();

                // itero todos los registros que no se han procesado todavía
                foreach (var ent in db.BibEntry.Where(b => b.EntryStatus == 1 && b.exclusion_json == null).ToList())
                {
                    string abstractField = ent.@abstract.Replace(Environment.NewLine, "").Replace("  ", " ");
                    string titleField = ent.title.Replace(Environment.NewLine, "").Replace("  ", " ");

                    // interpretación de chatgpt
                    string prompt = "Given the following info of an article:title:\"" + titleField + (abstractField != null ? ("\" and abstract:\"" + abstractField + "\"") : "") + " please give me a single number answer from 1 to 9, in JSON like {\"proximity\": <value>, \"reason\": <valueReason>}, in which 10 is the nearest and 1 is the less coincident, in determining the conceptual proximity of this article with the idea of the usage of the machine learning or deep learning techniques, for predicting or detecting automatically, the preeclampsia, complications and preganancy, hypertension and pregnancy, eclampsia, proteinuric hypertension of pregnancy, or toxaemia of pregnancy. A key element in evaluation should be the presence of preeclamptia or synonyms in the article.";

                    //https://github.com/dmitry-brazhenko/SharpToken
                    //https://github.com/openai/openai-cookbook/blob/main/examples/How_to_count_tokens_with_tiktoken.ipynb
                    var encoding = GptEncoding.GetEncoding("cl100k_base");
                    //var encoding = GptEncoding.GetEncodingForModel("gpt-3.5-turbo");
                    //var tokencount = messages.Sum(m => encoding.Encode("{role:" + m.role + ",content:" + m.content + "}").Count);
                    var tokencount = encoding.Encode("{role:user,content:" + prompt + "}").Count;

                    // API privado de Smartwork
                    string ApiKey = "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";

                    WebClient client = new WebClient();
                    client.Headers.Add("Authorization", "Bearer " + ApiKey);
                    client.Headers.Add("Content-Type", "application/json");
                    //https://platform.openai.com/docs/api-reference/completions
                    var request = new OpenAIChatRequest
                    {
                        Model = "gpt-5", //gpt-3.5-turbo - text-davinci-003 - code-davinci-002
                                                 //prompt = "hola, puedes decirme un saludo en 3 idiomas diferentes",

                        messages = new List<Message>()
                        {
                            //new message(){ role="user", content="mi empresa se llama smartwork. quiero que me ayudes generando un mensaje corto para la bienvenida de mi página web. mi empresa se dedica al desarrollo de software. el texto debe ser jovial y divertido, pero a la vez respetuoso."},
                            new Message(){ role="user", content=prompt},
                            //new message(){ role="system", content="en qué te puedo servir"},
                            //new message(){ role="user", content=system.io.file.readalltext(@"c:\log\demo.sql") }//"para qué sirve la sentencia yield en el lenguaje c#?"},
                        },
                        Temperature = 0.8f,
                        //MaxTokens = 3000
                        MaxTokens = 4097 - tokencount
                    };

                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                    try
                    {
                        //https://api.openai.com/v1/completions
                        //https://api.openai.com/v1/chat/completions
                        //var result = client.UploadData("https://api.openai.com/v1/completions", "POST", System.Text.Encoding.UTF8.GetBytes(data));
                        var result = client.UploadData("https://api.openai.com/v1/chat/completions", "POST", System.Text.Encoding.UTF8.GetBytes(data));

                        var resultJson = System.Text.Encoding.UTF8.GetString(result);

                        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenAIChatResponse>(resultJson);
                        var chatresponse = response.choices.First().message.content.Trim();

                        // se procesa el JSON esperado de calificación
                        ent.exclusion_json = chatresponse;
                    }
                    catch (WebException exception)
                    {
                        string responseText = "";

                        if (exception.Response != null)
                        {
                            using (var reader = new StreamReader(exception.Response.GetResponseStream()))
                            {
                                responseText = reader.ReadToEnd();
                            }
                        }

                        ent.exclusion_json = "error: " + responseText;
                    }

                    db.SaveChanges();

                    // mover el mouse para evitar que se bloquee la pantalla
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(rnd.Next(10, 500), rnd.Next(10, 500));
                }
            }
        }

        private static void chatGPT_Exclusion_Process()
        {
            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                // Procesa la proximidad
                foreach (var ent in db.BibEntry.Where(b => b.EntryStatus == 1 && b.proximity.HasValue == false).ToList())
                {
                    var proximityData = Newtonsoft.Json.JsonConvert.DeserializeObject<ProximityGPT>(ent.exclusion_json);
                    ent.proximity = Convert.ToInt32(Math.Round(proximityData.proximity, 0));
                    if (proximityData.proximity < 7)
                    {
                        ent.EntryStatus = 17; // excluido por no estar próximo al estudio en curso
                    }

                    db.SaveChanges();
                }
            }
        }


        private static void chatGPT_RQ1_Run()
        {
            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                Random rnd = new Random();

                // itero todos los registros que no se han procesado todavía
                foreach (var ent in db.BibEntry.Where(b => b.EntryStatus == 1 && b.rq1_json == null).ToList())
                {
                    string abstractField = ent.@abstract.Replace(Environment.NewLine, "").Replace("  ", " ");
                    string titleField = ent.title.Replace(Environment.NewLine, "").Replace("  ", " ");

                    // interpretación de chatgpt
                    string prompt = "Given the following info of an article:title:\"" + titleField + (abstractField != null ? ("\" and abstract:\"" + abstractField + "\"") : "") + ";please help me to identify the main machine learning, artificial intelligence or deep learning algorithms or models that were used during the study. If there is more than one identified, please separate with commas. Please in JSON using this format {\"methods\":\"<method_list>\"}";

                    //https://github.com/dmitry-brazhenko/SharpToken
                    //https://github.com/openai/openai-cookbook/blob/main/examples/How_to_count_tokens_with_tiktoken.ipynb
                    var encoding = GptEncoding.GetEncoding("cl100k_base");
                    //var encoding = GptEncoding.GetEncodingForModel("gpt-3.5-turbo");
                    //var tokencount = messages.Sum(m => encoding.Encode("{role:" + m.role + ",content:" + m.content + "}").Count);
                    var tokencount = encoding.Encode("{role:user,content:" + prompt + "}").Count;

                    // API privado de Smartwork
                    string ApiKey = "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";

                    WebClient client = new WebClient();
                    client.Headers.Add("Authorization", "Bearer " + ApiKey);
                    client.Headers.Add("Content-Type", "application/json");
                    //https://platform.openai.com/docs/api-reference/completions
                    var request = new OpenAIChatRequest
                    {
                        Model = "gpt-3.5-turbo", //gpt-3.5-turbo - text-davinci-003 - code-davinci-002
                                                 //prompt = "hola, puedes decirme un saludo en 3 idiomas diferentes",

                        messages = new List<Message>()
                        {
                            //new message(){ role="user", content="mi empresa se llama smartwork. quiero que me ayudes generando un mensaje corto para la bienvenida de mi página web. mi empresa se dedica al desarrollo de software. el texto debe ser jovial y divertido, pero a la vez respetuoso."},
                            new Message(){ role="user", content=prompt},
                            //new message(){ role="system", content="en qué te puedo servir"},
                            //new message(){ role="user", content=system.io.file.readalltext(@"c:\log\demo.sql") }//"para qué sirve la sentencia yield en el lenguaje c#?"},
                        },
                        Temperature = 0.8f,
                        //MaxTokens = 3000
                        MaxTokens = 4090 - tokencount
                    };

                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                    try
                    {
                        //https://api.openai.com/v1/completions
                        //https://api.openai.com/v1/chat/completions
                        //var result = client.UploadData("https://api.openai.com/v1/completions", "POST", System.Text.Encoding.UTF8.GetBytes(data));
                        var result = client.UploadData("https://api.openai.com/v1/chat/completions", "POST", System.Text.Encoding.UTF8.GetBytes(data));

                        var resultJson = System.Text.Encoding.UTF8.GetString(result);

                        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenAIChatResponse>(resultJson);
                        var chatresponse = response.choices.First().message.content.Trim();

                        // se procesa el JSON esperado de calificación
                        ent.rq1_json = chatresponse;
                    }
                    catch (WebException exception)
                    {
                        string responseText = "";

                        if (exception.Response != null)
                        {
                            using (var reader = new StreamReader(exception.Response.GetResponseStream()))
                            {
                                responseText = reader.ReadToEnd();
                            }
                        }

                        ent.rq1_json = "error: " + responseText;
                    }

                    db.SaveChanges();

                    // mover el mouse para evitar que se bloquee la pantalla
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(rnd.Next(10, 500), rnd.Next(10, 500));

                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        private static void chatGPT_RQ2_Run()
        {
            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                Random rnd = new Random();

                // itero todos los registros que no se han procesado todavía
                foreach (var ent in db.BibEntry.Where(b => b.EntryStatus == 1 && b.rq2_json == null).ToList())
                {
                    string abstractField = ent.@abstract.Replace(Environment.NewLine, "").Replace("  ", " ");
                    string titleField = ent.title.Replace(Environment.NewLine, "").Replace("  ", " ");

                    // interpretación de chatgpt
                    string prompt = "Given the following info of an article:title:\"" + titleField + (abstractField != null ? ("\" and abstract:\"" + abstractField + "\"") : "") + ";please help me to identify the key performance indicators, results efficiency info or diverse related metrics achieved from the usage of machine learning, artificial intelligence or deep learning algorithms / models of this study, mainly focusing on the prediction capabilities results. It is needed the numeric metric number or percentages presented inside the detected text. if undefined, please set this in the result. If there is more than one metric identified, please separate with commas. Please in JSON using this format {\"metric\":\"<metric_list>\"}";

                    //https://github.com/dmitry-brazhenko/SharpToken
                    //https://github.com/openai/openai-cookbook/blob/main/examples/How_to_count_tokens_with_tiktoken.ipynb
                    var encoding = GptEncoding.GetEncoding("cl100k_base");
                    //var encoding = GptEncoding.GetEncodingForModel("gpt-3.5-turbo");
                    //var tokencount = messages.Sum(m => encoding.Encode("{role:" + m.role + ",content:" + m.content + "}").Count);
                    var tokencount = encoding.Encode("{role:user,content:" + prompt + "}").Count;

                    // API privado de Smartwork
                    string ApiKey = "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";

                    WebClient client = new WebClient();
                    client.Headers.Add("Authorization", "Bearer " + ApiKey);
                    client.Headers.Add("Content-Type", "application/json");
                    //https://platform.openai.com/docs/api-reference/completions
                    var request = new OpenAIChatRequest
                    {
                        Model = "gpt-3.5-turbo", //gpt-3.5-turbo - text-davinci-003 - code-davinci-002
                                                 //prompt = "hola, puedes decirme un saludo en 3 idiomas diferentes",

                        messages = new List<Message>()
                        {
                            //new message(){ role="user", content="mi empresa se llama smartwork. quiero que me ayudes generando un mensaje corto para la bienvenida de mi página web. mi empresa se dedica al desarrollo de software. el texto debe ser jovial y divertido, pero a la vez respetuoso."},
                            new Message(){ role="user", content=prompt},
                            //new message(){ role="system", content="en qué te puedo servir"},
                            //new message(){ role="user", content=system.io.file.readalltext(@"c:\log\demo.sql") }//"para qué sirve la sentencia yield en el lenguaje c#?"},
                        },
                        Temperature = 0.8f,
                        //MaxTokens = 3000
                        MaxTokens = 4090 - tokencount
                    };

                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                    try
                    {
                        //https://api.openai.com/v1/completions
                        //https://api.openai.com/v1/chat/completions
                        //var result = client.UploadData("https://api.openai.com/v1/completions", "POST", System.Text.Encoding.UTF8.GetBytes(data));
                        var result = client.UploadData("https://api.openai.com/v1/chat/completions", "POST", System.Text.Encoding.UTF8.GetBytes(data));

                        var resultJson = System.Text.Encoding.UTF8.GetString(result);

                        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenAIChatResponse>(resultJson);
                        var chatresponse = response.choices.First().message.content.Trim();

                        // se procesa el JSON esperado de calificación
                        ent.rq2_json = chatresponse;
                    }
                    catch (WebException exception)
                    {
                        string responseText = "";

                        if (exception.Response != null)
                        {
                            using (var reader = new StreamReader(exception.Response.GetResponseStream()))
                            {
                                responseText = reader.ReadToEnd();
                            }
                        }

                        ent.rq2_json = "error: " + responseText;
                    }

                    db.SaveChanges();

                    // mover el mouse para evitar que se bloquee la pantalla
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(rnd.Next(10, 500), rnd.Next(10, 500));

                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        private static void chatGPT_RQ3_Run()
        {
            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                Random rnd = new Random();

                // itero todos los registros que no se han procesado todavía
                foreach (var ent in db.BibEntry.Where(b => b.EntryStatus == 1 && b.rq3_json == null).ToList())
                {
                    string abstractField = ent.@abstract.Replace(Environment.NewLine, "").Replace("  ", " ");
                    string titleField = ent.title.Replace(Environment.NewLine, "").Replace("  ", " ");

                    // interpretación de chatgpt
                    string prompt = "Given the following info of an article:title:\"" + titleField + (abstractField != null ? ("\" and abstract:\"" + abstractField + "\"") : "") + ";please help me to identify the technique of evaluation used for obtaining key performance indicators, results efficiency info or diverse related metrics achieved from the usage of machine learning, artificial intelligence or deep learning algorithms / models of this study, mainly focusing on the prediction capabilities results. It is needed the identification of the evaluation technique used. if undefined, please set this in the result. If there is more than one technique identified, please separate with commas. Please in JSON using this format {\"technique\":\"<techniquelist>\"}";

                    //https://github.com/dmitry-brazhenko/SharpToken
                    //https://github.com/openai/openai-cookbook/blob/main/examples/How_to_count_tokens_with_tiktoken.ipynb
                    var encoding = GptEncoding.GetEncoding("cl100k_base");
                    //var encoding = GptEncoding.GetEncodingForModel("gpt-3.5-turbo");
                    //var tokencount = messages.Sum(m => encoding.Encode("{role:" + m.role + ",content:" + m.content + "}").Count);
                    var tokencount = encoding.Encode("{role:user,content:" + prompt + "}").Count;

                    // API privado de Smartwork
                    string ApiKey = "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";

                    WebClient client = new WebClient();
                    client.Headers.Add("Authorization", "Bearer " + ApiKey);
                    client.Headers.Add("Content-Type", "application/json");
                    //https://platform.openai.com/docs/api-reference/completions
                    var request = new OpenAIChatRequest
                    {
                        Model = "gpt-3.5-turbo", //gpt-3.5-turbo - text-davinci-003 - code-davinci-002
                                                 //prompt = "hola, puedes decirme un saludo en 3 idiomas diferentes",

                        messages = new List<Message>()
                        {
                            //new message(){ role="user", content="mi empresa se llama smartwork. quiero que me ayudes generando un mensaje corto para la bienvenida de mi página web. mi empresa se dedica al desarrollo de software. el texto debe ser jovial y divertido, pero a la vez respetuoso."},
                            new Message(){ role="user", content=prompt},
                            //new message(){ role="system", content="en qué te puedo servir"},
                            //new message(){ role="user", content=system.io.file.readalltext(@"c:\log\demo.sql") }//"para qué sirve la sentencia yield en el lenguaje c#?"},
                        },
                        Temperature = 0.8f,
                        //MaxTokens = 3000
                        MaxTokens = 4090 - tokencount
                    };

                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                    try
                    {
                        //https://api.openai.com/v1/completions
                        //https://api.openai.com/v1/chat/completions
                        //var result = client.UploadData("https://api.openai.com/v1/completions", "POST", System.Text.Encoding.UTF8.GetBytes(data));
                        var result = client.UploadData("https://api.openai.com/v1/chat/completions", "POST", System.Text.Encoding.UTF8.GetBytes(data));

                        var resultJson = System.Text.Encoding.UTF8.GetString(result);

                        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenAIChatResponse>(resultJson);
                        var chatresponse = response.choices.First().message.content.Trim();

                        // se procesa el JSON esperado de calificación
                        ent.rq3_json = chatresponse;
                    }
                    catch (WebException exception)
                    {
                        string responseText = "";

                        if (exception.Response != null)
                        {
                            using (var reader = new StreamReader(exception.Response.GetResponseStream()))
                            {
                                responseText = reader.ReadToEnd();
                            }
                        }

                        ent.rq3_json = "error: " + responseText;
                    }

                    db.SaveChanges();

                    // mover el mouse para evitar que se bloquee la pantalla
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(rnd.Next(10, 500), rnd.Next(10, 500));

                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        private static void chatGPT_RQ4_Run()
        {
            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                Random rnd = new Random();

                // itero todos los registros que no se han procesado todavía
                foreach (var ent in db.BibEntry.Where(b => b.EntryStatus == 1 && b.rq4_json == null).ToList())
                {
                    string abstractField = ent.@abstract.Replace(Environment.NewLine, "").Replace("  ", " ");
                    string titleField = ent.title.Replace(Environment.NewLine, "").Replace("  ", " ");

                    // interpretación de chatgpt
                    string prompt = "Given the following info of an article:title:\"" + titleField + (abstractField != null ? ("\" and abstract:\"" + abstractField + "\"") : "") + ";Considering that this article have been selected because its approach to the usage of machine learning, artificial intelligence or deep learning algorithms, for predicting preeclampsia. It is needed to identify if during the study it was used a public or private data repository, and if defined, extract the reference of this repository. if undefined, please set this in the result. If there is more than one repository identified, please separate with commas. Please in JSON using this format {\"repository\":\"<repositorylist>\"}";

                    //https://github.com/dmitry-brazhenko/SharpToken
                    //https://github.com/openai/openai-cookbook/blob/main/examples/How_to_count_tokens_with_tiktoken.ipynb
                    var encoding = GptEncoding.GetEncoding("cl100k_base");
                    //var encoding = GptEncoding.GetEncodingForModel("gpt-3.5-turbo");
                    //var tokencount = messages.Sum(m => encoding.Encode("{role:" + m.role + ",content:" + m.content + "}").Count);
                    var tokencount = encoding.Encode("{role:user,content:" + prompt + "}").Count;

                    // API privado de Smartwork
                    string ApiKey = "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";

                    WebClient client = new WebClient();
                    client.Headers.Add("Authorization", "Bearer " + ApiKey);
                    client.Headers.Add("Content-Type", "application/json");
                    //https://platform.openai.com/docs/api-reference/completions
                    var request = new OpenAIChatRequest
                    {
                        Model = "gpt-3.5-turbo", //gpt-3.5-turbo - text-davinci-003 - code-davinci-002
                                                 //prompt = "hola, puedes decirme un saludo en 3 idiomas diferentes",

                        messages = new List<Message>()
                        {
                            //new message(){ role="user", content="mi empresa se llama smartwork. quiero que me ayudes generando un mensaje corto para la bienvenida de mi página web. mi empresa se dedica al desarrollo de software. el texto debe ser jovial y divertido, pero a la vez respetuoso."},
                            new Message(){ role="user", content=prompt},
                            //new message(){ role="system", content="en qué te puedo servir"},
                            //new message(){ role="user", content=system.io.file.readalltext(@"c:\log\demo.sql") }//"para qué sirve la sentencia yield en el lenguaje c#?"},
                        },
                        Temperature = 0.8f,
                        //MaxTokens = 3000
                        MaxTokens = 4090 - tokencount
                    };

                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                    try
                    {
                        //https://api.openai.com/v1/completions
                        //https://api.openai.com/v1/chat/completions
                        //var result = client.UploadData("https://api.openai.com/v1/completions", "POST", System.Text.Encoding.UTF8.GetBytes(data));
                        var result = client.UploadData("https://api.openai.com/v1/chat/completions", "POST", System.Text.Encoding.UTF8.GetBytes(data));

                        var resultJson = System.Text.Encoding.UTF8.GetString(result);

                        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenAIChatResponse>(resultJson);
                        var chatresponse = response.choices.First().message.content.Trim();

                        // se procesa el JSON esperado de calificación
                        ent.rq4_json = chatresponse;
                    }
                    catch (WebException exception)
                    {
                        string responseText = "";

                        if (exception.Response != null)
                        {
                            using (var reader = new StreamReader(exception.Response.GetResponseStream()))
                            {
                                responseText = reader.ReadToEnd();
                            }
                        }

                        ent.rq4_json = "error: " + responseText;
                    }

                    db.SaveChanges();

                    // mover el mouse para evitar que se bloquee la pantalla
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(rnd.Next(10, 500), rnd.Next(10, 500));

                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        private static void chatGPT_Classification_Run()
        {
            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                Random rnd = new Random();

                // itero todos los registros que no se han procesado todavía
                foreach (var ent in db.BibEntry.Where(b => b.EntryStatus == 1 && b.classification == null).ToList())
                {
                    string abstractField = ent.@abstract.Replace(Environment.NewLine, "").Replace("  ", " ");
                    string titleField = ent.title.Replace(Environment.NewLine, "").Replace("  ", " ");

                    // interpretación de chatgpt
                    string prompt = "Given article info:title:\"" + titleField + (abstractField != null ? ("\" and abstract:\"" + abstractField + "\"") : "") + ";Give the tag name of the classification tag it could be mainly related. In JSON like {\"tag\":<value>}.The classification tags are the following: 1.Machine Learning Approaches: 'machine learning', 'algorithm', or specific methods like 'XGBoost' or 'LightGBM'.2.Deep Learning Approaches: 'Deep Learning', 'Neural Networks', or 'Convolutional Neural Networks'.3.Genetic and Biomarker Analysis: 'biomarkers', 'genetic', 'Single Nucleotide Polymorphism', or specific genes and molecules like 'sFlt-1', 'PlGF', etc.4.Imaging and Ultrasound Techniques: 'ultrasound', 'ultrasonic image features', 'placental whole-slide images', or 'radiomics'.5.Natural Language Processing (NLP): 'natural language processing' for predictions, like admission notes.6.Chatbots and Cyber-Physical Systems: 'chatbot' or 'cyber-physical platform'.7.Clinical and Laboratory Indicators: 'routine laboratory indicators', 'maternal characteristics', 'blood lipidome', etc.8.Reviews and Meta-Analysis: 'review', 'systematic review', or 'meta-analysis'.9.Dietary and Nutritional Analysis: 'dietary synergy' or 'dietary intake'.10.Integrated Multi-Omics Analysis: 'multiomics' or a combination of various 'omics' techniques.11.Data Mining and Database Analysis: 'data mining' or specific databases like 'nationwide health insurance dataset'.12.Risk and Severity Analysis: 'risk factors', 'severity', 'prognostic models', or 'risk assessment'.13.Prospective and Retrospective Studies: 'prospective study', 'retrospective study', or 'cohort study'.14.Special Populations: specific populations or conditions, like 'COVID-19 pandemic', 'gestational diabetes', or 'nulliparous women'.15.Integration with Clinical Practice: integration of AI in clinical settings, like 'clinical decision support', 'clinical risk assessment', or 'standardizing quality of care'.";

                    //https://github.com/dmitry-brazhenko/SharpToken
                    //https://github.com/openai/openai-cookbook/blob/main/examples/How_to_count_tokens_with_tiktoken.ipynb
                    var encoding = GptEncoding.GetEncoding("cl100k_base");
                    //var encoding = GptEncoding.GetEncodingForModel("gpt-3.5-turbo");
                    //var tokencount = messages.Sum(m => encoding.Encode("{role:" + m.role + ",content:" + m.content + "}").Count);
                    var tokencount = encoding.Encode("{role:user,content:" + prompt + "}").Count;

                    // API privado de Smartwork
                    string ApiKey = "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";

                    WebClient client = new WebClient();
                    client.Headers.Add("Authorization", "Bearer " + ApiKey);
                    client.Headers.Add("Content-Type", "application/json");
                    //https://platform.openai.com/docs/api-reference/completions
                    var request = new OpenAIChatRequest
                    {
                        Model = "gpt-3.5-turbo", //gpt-3.5-turbo - text-davinci-003 - code-davinci-002
                                                 //prompt = "hola, puedes decirme un saludo en 3 idiomas diferentes",

                        messages = new List<Message>()
                        {
                            //new message(){ role="user", content="mi empresa se llama smartwork. quiero que me ayudes generando un mensaje corto para la bienvenida de mi página web. mi empresa se dedica al desarrollo de software. el texto debe ser jovial y divertido, pero a la vez respetuoso."},
                            new Message(){ role="user", content=prompt},
                            //new message(){ role="system", content="en qué te puedo servir"},
                            //new message(){ role="user", content=system.io.file.readalltext(@"c:\log\demo.sql") }//"para qué sirve la sentencia yield en el lenguaje c#?"},
                        },
                        Temperature = 0.8f,
                        //MaxTokens = 3000
                        MaxTokens = 4090 - tokencount
                    };

                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                    try
                    {
                        //https://api.openai.com/v1/completions
                        //https://api.openai.com/v1/chat/completions
                        //var result = client.UploadData("https://api.openai.com/v1/completions", "POST", System.Text.Encoding.UTF8.GetBytes(data));
                        var result = client.UploadData("https://api.openai.com/v1/chat/completions", "POST", System.Text.Encoding.UTF8.GetBytes(data));

                        var resultJson = System.Text.Encoding.UTF8.GetString(result);

                        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenAIChatResponse>(resultJson);
                        var chatresponse = response.choices.First().message.content.Trim();

                        // se procesa el JSON esperado de calificación
                        ent.classification = chatresponse;
                    }
                    catch (WebException exception)
                    {
                        string responseText = "";

                        if (exception.Response != null)
                        {
                            using (var reader = new StreamReader(exception.Response.GetResponseStream()))
                            {
                                responseText = reader.ReadToEnd();
                            }
                        }

                        ent.classification = "error: " + responseText;
                    }

                    db.SaveChanges();

                    // mover el mouse para evitar que se bloquee la pantalla
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(rnd.Next(10, 500), rnd.Next(10, 500));

                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        private static void MetodosAntiguos()
        {
            //Limpieza de los archivos cargados. La librería solamente lee si es que el tipo de artículo está en minúscula.
            //solamente lee si hay espacios alrededor del " = " en el archivo bib
            //las bases de origen entregan la información de esta manera. La libreria tiene estas limitaciones de control
            //ProcesarLimpieza();

            // Carga inicial a la base de datos. Lee el Bibtex y lo graba en la tabla
            //CargaBibTeXaBase();

            // Limpia los datos, varias columnas se leen al final con un "}, "
            //LimpiezaPostCarga();

            // Marca de Duplicados, marca registros sin doi y otros post-procesamientos de limpmieza hechos en SP en la base de datos
            //DeteccionDuplicados();

            // Enriquecimiento Cross Ref API
            try
            {
                // Primero descarga los datos para todos los artículos en JSON, sin procesar
                //DownloadArticleInfo();

                // cuando ya está grabado en la base de datos, procesa todos los JSON para extraer la información
                //ProcessArticleInfo();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                Console.WriteLine(ex.EntityValidationErrors.First().ValidationErrors.First().ErrorMessage);
            }

            // Enriquecimiento con BibTex Converter
            // se extrae la lista de dois directamente de la base de datos
            // Obtención de Bibtex desde CrossRef Bibtex
            MejoraDatosBibtex();

            // Enriquecimiento con DOI to BibTeX converter 
            // ya no hace falta, la información hasta este punto está bastante completa


            // Generación de archivos para enviar a Atlas
            // esto sirve para sacar el word list y el word cloud

            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                foreach (var article in db.BibEntry.Where(b => b.EntryStatus == 1).ToList())
                {
                    string FolderPath = @"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Papers Base R2\Procesador\ProcesadorBib\exportedTXT\" + article.EntryID + ".txt";
                    System.IO.File.WriteAllText(FolderPath, article.title + Environment.NewLine + article.@abstract);
                }
            }



            //imbSCI.BibTex.BibTexTools.ExportToExcel(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Papers Base R2\scopus_P1.txt", new imbSCI.Core.data.aceAuthorNotation());
            //LoadSource(System.IO.File.ReadAllText(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Papers Base R2\springer_rbib.bib"));
            //var s = new imbSCI.BibTex.BibTexDataFile(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Ejecucion\ACM.bib");
            //string cols = s.fields.Aggregate((a, b) => a + " varchar(100) null," + Environment.NewLine + b);

            //using (DOC_TmpEntities db = new DOC_TmpEntities())
            //{
            //    foreach (var ent in s.UntypedEntries)
            //    {
            //        if (!ent.Tags.ContainsKey("doi")) continue;
            //        if (string.IsNullOrEmpty(ent.Key)) continue;
            //        string doi = ent.Tags["doi"].Value;
            //        doi = doi.Replace("},\r", "");
            //        var entry = db.BibEntry.FirstOrDefault(b => b.doi.Trim() == doi.Trim() && b.db == "springer");
            //        if (entry == null)
            //        {
            //            System.IO.File.AppendAllText(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Papers Base R2\springerlog.txt",
            //                doi + Environment.NewLine);
            //            continue;
            //        }
            //        entry.EntryKey = ent.Key;
            //    }
            //    db.SaveChanges();
            //}


            // Obtención de Bibtex desde CrossRef Bibtex

            //var str = @"10.1145/3196494.3196522, 10.1145/3197978, 10.1145/2464996.2465007,10.1145/3040990, 10.1109/WOLFHPC.2014.11, 10.1631/fitee.1601660,10.1145/3196494.3196522, 10.1145/3363562, 10.1145/2755561,10.1145/2517349.2522715, 10.1145/2508859.2516752, 10.1145/2988336.2988348,10.1145/2400682.2400713, 10.1109/TPDS.2015.2394802, 10.1145/2976767.2976812,10.1145/2400682.2400713, 10.1145/2435264.2435273, 10.1145/2503210.2503268,10.1145/3363562, 10.1145/2594291.2594316, 10.1145/3290380,10.1145/3243734.3243790, 10.1007/s11036-012-0374-2, 10.1145/3196398.3196464,10.1145/2435264.2435273, 10.1145/2568225.2568297, 10.1145/2517349.2522715,10.1145/2584660, 10.1007/978-3-642-40698-0_7, 10.1145/3381039,10.1145/2788396, 10.1145/3220134.3220135, 10.1145/2480362.2480464,10.14778/2732951.2732959, 10.1145/3341700, 10.1145/2660252.2664662,10.1145/2788396, 10.1145/2400682.2400713, 10.1145/3196494.3196522,10.1145/2788396, 10.1145/2435264.2435273, 10.1145/2517349.2522715,10.1145/3198458.3198464, 10.1007/s00779-013-0682-y, 10.1109/CCGRID.2017.142,10.1145/2464996.2465007, 10.1007/978-3-642-40698-0_7, 10.1007/s10270-018-0675-4,10.1145/2560359, 10.1145/2503210.2503268, 10.1145/2766895,10.1145/2568225.2568297, 10.1145/3220134.3220135, 10.1145/2503210.2503281,10.1145/3196494.3196522, 10.1145/3040990, 10.1145/2508859.2516744,10.1145/3135974.3135979, 10.1145/2508859.2516670, 10.1631/fitee.1601660,10.1145/2435264.2435271, 10.1145/3352460.3358313, 10.1145/2479871.2479892,10.1145/2788396, 10.1145/2400682.2400713, 10.1007/s11227-013-0884-0,10.1145/2788396, 10.1007/s11227-013-0884-0, 10.1145/2435264.2435273,10.1145/2788396, 10.1145/2517349.2522715, 10.1145/2503210.2503281,10.1145/2568225.2568297, 10.1145/2560359, 10.1145/2517349.2522715,10.1145/3352460.3358313, 10.1145/3400302.3415620, 10.1109/TCAD.2020.3032630,10.1145/2568225.2568297, 10.1145/3308558.3313591, 10.1145/2435264.2435271,10.1145/2950290.2950350, 10.1145/3198458.3198464, 10.1145/3014426,10.1007/978-3-319-16295-9_4, 10.1145/3243218.3243220, 10.1007/978-1-4842-5352-6_12,10.1145/3198458.3198464, 10.1007/s11227-015-1483-z, 10.1109/ICSE.2017.70,10.1145/2400682.2400713, 10.1109/TPDS.2015.2394802, 10.1109/MICRO.2014.20,10.1145/2435264.2435273, 10.1145/2500365.2500615, 10.1007/s10766-012-0209-6";
            //var lst = str.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            //string bibtex = "";
            //foreach(string item in lst)
            //{
            //    using (System.Net.WebClient client = new System.Net.WebClient())
            //    {
            //        string data = client.DownloadString("https://api.crossref.org/works/" + item + "/transform/application/x-bibtex");
            //        bibtex += data + Environment.NewLine;
            //    }

            //}
            //System.IO.File.WriteAllText(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Principal.bib", bibtex);







            //using (DOC_TmpEntities db = new DOC_TmpEntities())
            //{
            //    var res = db.Doc_Tags().ToList();
            //    var xrd = res.Where(r => r.tagv == "mobile" || r.tagw == "mobile");

            //    Guid rootid = Guid.NewGuid();
            //    //var tags = res.FirstOrDefault(t => t.tagv == "transpiler" || t.tagw == "transpiler");
            //    TagItem root = new TagItem() { ID = rootid, tagdata = "transpiler", parentID = Guid.Empty, tagparent = "" };
            //    List<TagItem> lst = new List<TagItem>();
            //    lst.Add(root);

            //    // metodo recursivo para determinar la cadena del transpilador
            //    NextLevel("transpiler", lst, res.ToList(), rootid);

            //    List<string> phrases = new List<string>();
            //    // escritura de las frases para el graficador
            //    WriteLeaf(rootid, lst, phrases);

            //    string wordtree = "";
            //    foreach (var phrase in phrases)
            //    {
            //        wordtree += "['" + phrase + "']," + Environment.NewLine;
            //    }



            //    string orgchart = "";
            //    foreach (var item in lst)
            //    {
            //        orgchart += "['" + item.tagdata + "','" + item.tagparent +"','']," + Environment.NewLine;
            //    }

            //    int a = 0;
            //}

            //using (DOC_TmpEntities db = new DOC_TmpEntities())
            //{
            //    foreach (var ent in db.BibEntry.Where(entry => entry.db == "springer" && entry.EntryKey != null).ToList())
            //    {
            //        using (System.Net.WebClient client = new System.Net.WebClient())
            //        {
            //            //string data = client.DownloadString("https://link.springer.com/chapter/10.1007/978-3-030-58799-4_47");

            //            //client.Headers.Add("Accept", "application/vnd.crossref.unixref+xml");
            //            //string data = client.DownloadString("http://dx.crossref.org/10.1007/s11227-019-03109-9");

            //            string data = client.DownloadString("http://link.springer.com/" + ent.doi);

            //            int pos = data.IndexOf("Abs1-content");
            //            if (pos < 0) continue;
            //            pos = data.IndexOf("<p>", pos);
            //            int end = data.IndexOf("</p>", pos);
            //            string abs = data.Substring(pos + "<p>".Length, end - pos - 3);
            //            //"Abs1-content"

            //            ent.@abstract = abs;
            //        }
            //        db.SaveChanges();
            //    }
            //}




            ////var w = s.UntypedEntries.First();
            ////int x = 1;
        }

        private static void MejoraDatosBibtex()
        {
            string path = @"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Ejecucion\Enrichment.bib";

            StringBuilder sbBibtex = new StringBuilder();
            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                //foreach (var ent in db.BibEntry.Where(b => b.EntryStatus == 1).ToList())
                //{
                //    using (System.Net.WebClient client = new System.Net.WebClient())
                //    {
                //        string contents = client.DownloadString("https://api.crossref.org/works/" + ent.doi + "/transform/application/x-bibtex");

                //        // hace los reemplazos
                //        contents = contents.Replace("@INPROCEEDINGS", "@inproceedings");
                //        contents = contents.Replace("@ARTICLE", "@article");
                //        contents = contents.Replace("@CONFERENCE", "@inproceedings");
                //        contents = contents.Replace("@BOOK", "@book");
                //        contents = contents.Replace("@PROCEEDINGS", "@inproceedings");

                //        // para las expresiones que tienen un igual, sin espacios, le pone
                //        Regex r = new Regex(@"(?<r1>[^\x20])=(?<r2>[^\x20])");
                //        string clean = r.Replace(contents, "${r1} = ${r2}");

                //        sbBibtex.Append(clean + Environment.NewLine);
                //    }
                //}

                //// Graba el archivo para que luego pueda ser procesado
                //// Nota: para que lea bien, hay que eliminar las tabulaciones
                //System.IO.File.WriteAllText(path, sbBibtex.ToString().Replace("\t", ""));

                var s = new imbSCI.BibTex.BibTexDataFile(path);

                foreach (var ent in s.UntypedEntries)
                {
                    if (ent.Tags["doi"] == null) continue;
                    var doi = ent.Tags["doi"].Value;

                    var entry = db.BibEntry.FirstOrDefault(b => b.doi == doi && b.EntryStatus == 1);

                    if (entry == null) continue;

                    if (entry.EntryType == null && ent.type != null)
                        entry.EntryType = ent.type;

                    // El entrykey se pisa, dado que de esta fuente viene armado correctamente
                    //if (entry.EntryKey != null && ent.Key != null)
                    entry.EntryKey = ent.Key;

                    foreach (var tag in ent.Tags)
                    {
                        var field = typeof(BibEntry).GetProperty(tag.Value.Key);
                        if (field == null)
                            field = typeof(BibEntry).GetProperty(tag.Value.Key.ToUpper());

                        // si sigue null, es que no encuentra el campo en la tabla
                        if (field == null)
                        {
                            // no hay un campo, por lo tanto no se almacena simplemente
                            // pero se deja un registro de los campos que no existen
                            System.IO.File.AppendAllText(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Ejecucion\log.txt", tag.Value.Key + " no existe en la base de datos" + Environment.NewLine);
                        }
                        else
                        {
                            // reemplaza todo lo que puede, dado que viene con datos mejor estructurados
                            //if (field.GetValue(entry) == null && tag.Value.Value != null)
                            field.SetValue(entry, tag.Value.Value);
                        }

                        if (tag.Value.Key == "abstract")
                        {
                            if (entry.@abstract == null || entry.@abstract == "")
                            {
                                if (tag.Value.Value != null)
                                    entry.@abstract = tag.Value.Value;
                            }
                        }

                        // pisa la URL con la que viene del enriquecimiento
                        if (tag.Value.Key == "url")
                        {
                            entry.url = tag.Value.Value;
                        }
                    }
                }
                db.SaveChanges();
            }
        }



        private static void ProcesarLimpieza()
        {
            string path = @"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Ejecucion\ACM.bib";
            LimpiezaPreCarga(path);
            path = @"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Ejecucion\IEEEXplore.bib";
            LimpiezaPreCarga(path);
            path = @"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Ejecucion\ScienceDirect.bib";
            LimpiezaPreCarga(path);
            path = @"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Ejecucion\Scopus.bib";
            LimpiezaPreCarga(path);

        }

        private static void LimpiezaPreCarga(string path)
        {
            string contents = System.IO.File.ReadAllText(path);
            var fi = new System.IO.FileInfo(path);

            // genera respaldo
            System.IO.File.WriteAllText(fi.DirectoryName + "\\" + System.IO.Path.GetFileNameWithoutExtension(path) + "_backup" + DateTime.Now.ToString("yyyyMMddhhmm") + fi.Extension, contents);

            // hace los reemplazos
            contents = contents.Replace("@INPROCEEDINGS", "@inproceedings");
            contents = contents.Replace("@ARTICLE", "@article");
            contents = contents.Replace("@CONFERENCE", "@inproceedings");
            contents = contents.Replace("@BOOK", "@book");
            contents = contents.Replace("@PROCEEDINGS", "@inproceedings");

            // para las expresiones que tienen un igual, sin espacios, le pone
            Regex r = new Regex(@"(?<r1>[^\x20])=(?<r2>[^\x20])");
            string clean = r.Replace(contents, "${r1} = ${r2}");

            // escribe el archivo ajustado
            System.IO.File.WriteAllText(path, clean);
        }

        private static void LimpiezaPostCarga()
        {
            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                foreach (var ent in db.BibEntry.Where(b => b.EntryStatus == 1).ToList())
                {
                    if (ent.EntryType != null) ent.EntryType = ent.EntryType.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.EntryKey != null) ent.EntryKey = ent.EntryKey.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.@abstract != null) ent.@abstract = ent.@abstract.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.annote != null) ent.annote = ent.annote.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.booktitle != null) ent.booktitle = ent.booktitle.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.ISBN != null) ent.ISBN = ent.ISBN.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.publisher != null) ent.publisher = ent.publisher.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.title != null) ent.title = ent.title.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.url != null) ent.url = ent.url.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.year != null) ent.year = ent.year.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.doi != null) ent.doi = ent.doi.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.ISSN != null) ent.ISSN = ent.ISSN.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.volume != null) ent.volume = ent.volume.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.author != null) ent.author = ent.author.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.keywords != null) ent.keywords = ent.keywords.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.pages != null) ent.pages = ent.pages.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.month != null) ent.month = ent.month.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.journal != null) ent.journal = ent.journal.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.number != null) ent.number = ent.number.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.address != null) ent.address = ent.address.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.editor != null) ent.editor = ent.editor.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.archivePrefix != null) ent.archivePrefix = ent.archivePrefix.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.arxivId != null) ent.arxivId = ent.arxivId.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.eprint != null) ent.eprint = ent.eprint.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.series != null) ent.series = ent.series.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.db != null) ent.db = ent.db.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.edition != null) ent.edition = ent.edition.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.day != null) ent.day = ent.day.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.note != null) ent.note = ent.note.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.jsondata != null) ent.jsondata = ent.jsondata.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.articletype != null) ent.articletype = ent.articletype.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.cISSN != null) ent.cISSN = ent.cISSN.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.cISBN != null) ent.cISBN = ent.cISBN.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.containertitle != null) ent.containertitle = ent.containertitle.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.bibtex != null) ent.bibtex = ent.bibtex.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.articleno != null) ent.articleno = ent.articleno.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.numpages != null) ent.numpages = ent.numpages.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.location != null) ent.location = ent.location.Replace("},\r", "").Replace("},}\r", "");
                    if (ent.issue_date != null) ent.issue_date = ent.issue_date.Replace("},\r", "").Replace("},}\r", "");
                }
                db.Database.CommandTimeout = 60 * 60 * 5;
                db.SaveChanges();
            }
        }

        static void CargaBibTeXaBase()
        {
            // Carga de datos de BIBTEX a la base de datos
            List<BibEntry> dbentries = new List<BibEntry>();

            // ACM 186 registros.
            var s = new imbSCI.BibTex.BibTexDataFile(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Ejecucion\ACM.bib");
            MigrateBibtoDB(dbentries, s, "acm");

            // IEEEXplore - 246 registros
            s = new imbSCI.BibTex.BibTexDataFile(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Ejecucion\IEEEXplore.bib");
            MigrateBibtoDB(dbentries, s, "IEEEXplore");

            // ScienceDirect - 86 registros
            s = new imbSCI.BibTex.BibTexDataFile(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Ejecucion\ScienceDirect.bib");
            MigrateBibtoDB(dbentries, s, "ScienceDirect");

            // Scopus - 663 registros
            s = new imbSCI.BibTex.BibTexDataFile(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Ejecucion\Scopus.bib");
            MigrateBibtoDB(dbentries, s, "Scopus");

            // Total: 1180
            using (DOC_TmpEntities ctx = new DOC_TmpEntities())
            {
                ctx.Database.CommandTimeout = 60 * 60 * 5;
                ctx.BibEntry.AddRange(dbentries);
                ctx.SaveChanges();
            }

        }

        private static void MigrateBibtoDB(List<BibEntry> dbentries, BibTexDataFile s, string db)
        {
            foreach (var ent in s.UntypedEntries)
            {
                BibEntry ne = new BibEntry();
                ne.EntryID = Guid.NewGuid();
                ne.EntryType = ent.type;
                ne.EntryStatus = 1;
                ne.EntryKey = ent.Key;
                ne.bibtex = ent.source;
                ne.db = db;
                foreach (var tag in ent.Tags)
                {
                    var field = typeof(BibEntry).GetProperty(tag.Value.Key);
                    if (field == null)
                        field = typeof(BibEntry).GetProperty(tag.Value.Key.ToUpper());

                    // si sigue null, es que no encuentra el campo en la tabla
                    if (field == null)
                    {
                        // no hay un campo, por lo tanto no se almacena simplemente
                        // pero se deja un registro de los campos que no existen
                        System.IO.File.AppendAllText(@"C:\Onedrive\OneDrive - SMARTWORK SA\Archivos\Doctorado en Informática - EPN\Paper Revista 1\Ejecucion\log.txt", tag.Value.Key + " no existe en la base de datos" + Environment.NewLine);
                    }
                    else
                    {
                        field.SetValue(ne, tag.Value.Value);
                    }
                }
                dbentries.Add(ne);
            }
        }



        /// <summary>
        /// Regex select SplitEntries : ^@
        /// </summary>
        /// <remarks>
        /// <para>For text: example text</para>
        /// <para>Selects: ex</para>
        /// </remarks>
        public static Regex _select_SplitEntries = new Regex(@"^@", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Regex select keyAndTypeSelectionName : ^([\w]*)\{([\w\d]*)
        /// </summary>
        /// <remarks>
        /// <para>For text: example text</para>
        /// <para>Selects: ex</para>
        /// </remarks>
        public static Regex _select_keyAndTypeSelection = new Regex(@"^([\w]*)\{([\w\d]*)", RegexOptions.Compiled);

        /// <summary>
        /// Regex select SelectTags : ^([\w]*) = \{(.*)\},?
        /// </summary>
        /// <remarks>
        /// <para>For text: example text</para>
        /// <para>Selects: ex</para>
        /// </remarks>
        public static Regex _select_isSelectTags = new Regex(@"^([\w]*) = (.*),?", RegexOptions.Compiled | RegexOptions.Multiline);

        private static List<Char> sourceTrim = new List<char>() { '{', '}', '"', ',' };

        /// <summary>
        /// Loads Bibtex entries from the source code
        /// </summary>
        /// <param name="source">The BibTex string source code</param>
        /// <param name="log">The log.</param>
        public static void LoadSource(String source)
        {
            BibTexSourceProcessor processor = new BibTexSourceProcessor();
            translationTextTable processorTable = processor.latex;

            var sourceParts = _select_SplitEntries.Split(source);
            foreach (String entrySource in sourceParts)
            {
                Match mch = _select_keyAndTypeSelection.Match(entrySource);
                if (mch.Success)
                {
                    var lastType = mch.Groups[1].Value;
                    var lastKey = mch.Groups[2].Value;
                    var bEB = new BibTexEntryBase(entrySource, lastType, lastKey, processorTable);

                    //UntypedEntries.Add(bEB);

                    foreach (Match mcht in _select_isSelectTags.Matches(entrySource.Trim(sourceTrim.ToArray())))
                    {
                        String sourcet = mch.Groups[2].Value.Trim(sourceTrim.ToArray());

                        BibTexEntryTag tmp = new BibTexEntryTag(mcht.Groups[1].Value, sourcet);
                        tmp.source = sourcet;
                        //AddTag(tmp.Key, tmp);
                    }
                }
            }

            //foreach (var bED in UntypedEntries)
            //{
            //    foreach (BibTexEntryTag bEDt in bED.Tags.Values)
            //    {
            //        if (!fields.Contains(bEDt.Key))
            //        {
            //            fields.Add(bEDt.Key);
            //        }
            //    }
            //}

        }

        static void NextLevel(string parent, List<TagItem> lst, List<Doc_Tags_Result> res, Guid parentID)
        {
            // filtro el siguiente nivel en base al termino, en ambas columnas
            var ftr = res.Where(t => t.tagv == parent || t.tagw == parent);

            // itero por cada item y lo agrego a la lista, si es que es hijo
            foreach (var item in ftr)
            {
                // determino el nombre del tag nuevo
                var tagname = item.tagv != parent ? item.tagv : item.tagw;

                // valido si es que este tag ya no esta antes en la cadena
                //var innertags = lst.Where()
                var current = lst.First(t => t.tagdata == parent);
                var found = false;
                while (true)
                {
                    if (current.tagdata == tagname)
                        found = true;

                    if (current.tagparent == "")
                        break;

                    current = lst.First(t => t.tagdata == current.tagparent);
                }
                //if (lst.Count(t => t.tagdata == tagname) > 0)
                //    continue;

                if (found) continue;

                // agrego a la lista
                TagItem ntag = new TagItem() { ID = Guid.NewGuid(), tagdata = tagname, parentID = parentID, tagparent = parent };
                lst.Add(ntag);

                // busco el siguiente nivel
                NextLevel(tagname, lst, res, ntag.ID);
            }
        }

        static void WriteLeaf(Guid currentID, List<TagItem> lst, List<string> phrases)
        {
            // tomo el item actual
            var current = lst.First(t => t.ID == currentID);

            // verifico si no tiene hijos, si no tiene, escribe la frase
            if (lst.Count(t => t.parentID == currentID) == 0)
            {
                // frase a escribirse
                string phrase = "";

                var actual = lst.First(t => t.ID == currentID);

                // escribe la frase con toda la ruta
                while (true)
                {
                    if (actual.parentID == Guid.Empty)
                        break;

                    // concatena
                    phrase = actual.tagdata + " " + phrase;

                    actual = lst.First(t => t.ID == actual.parentID);
                }

                // agrega la frase al listado final
                phrases.Add(phrase);
            }

            // navega al siguiente nivel entre los hijos
            var nextlevel = lst.Where(t => t.parentID == currentID);

            foreach (var item in nextlevel)
            {
                WriteLeaf(item.ID, lst, phrases);
            }
        }

        // DTOs para mapear la respuesta JSON estructurada
        private class Root
        {
            public RQ1 rq1 { get; set; }
            public RQ2 rq2 { get; set; }
            public RQ3 rq3 { get; set; }
            public RQ4 rq4 { get; set; }
            public RQ5 rq5 { get; set; }
            public RQ6 rq6 { get; set; }
            public RQ7 rq7 { get; set; }
            public string audit_note { get; set; }
        }

        private class RQ1
        {
            public List<string> model_families { get; set; }
            public List<string> prediction_targets { get; set; }
            public List<string> prediction_timing { get; set; }
            public List<string> input_modalities { get; set; }
            public string study_design { get; set; }
            public string setting_scope { get; set; }
            public string setting_country { get; set; }
            public int? sample_size_n { get; set; }
        }

        private class RQ2
        {
            public List<string> reported_metrics { get; set; }
            public decimal? auc { get; set; }
            public decimal? auc_ci_low { get; set; }
            public decimal? auc_ci_high { get; set; }
            public decimal? accuracy { get; set; }
            public decimal? sensitivity { get; set; }
            public decimal? specificity { get; set; }
            public decimal? ppv { get; set; }
            public decimal? npv { get; set; }
            public decimal? f1 { get; set; }
            public decimal? brier { get; set; }
            public decimal? hl_p { get; set; }
            public string internal_validation { get; set; }
            public bool? comparator_present { get; set; }
            public string comparator_type { get; set; }
        }

        private class RQ3
        {
            public bool? repository_mentioned { get; set; }
            public string repository_name { get; set; }
            public string access_type { get; set; }
            public List<string> data_types { get; set; }
        }

        private class RQ4
        {
            public List<string> feature_groups { get; set; }
            public string top_predictors { get; set; }
        }

        private class RQ5
        {
            public string real_world_design { get; set; }
            public decimal? real_world_auc { get; set; }
            public decimal? real_world_auc_ci_low { get; set; }
            public decimal? real_world_auc_ci_high { get; set; }
            public List<string> bias_risk_signals { get; set; }
        }

        private class RQ6
        {
            public bool? external_validation { get; set; }
            public string external_validation_site { get; set; }
            public List<string> techniques_compared { get; set; }
            public List<string> statistical_comparison { get; set; }
        }

        private class RQ7
        {
            public bool? explainability_used { get; set; }
            public List<string> explainability_methods { get; set; }
            public string clinical_use_of_explanations { get; set; }
        }

        private static string _jsonSchema = @"
{
  ""type"": ""object"",
  ""additionalProperties"": false,
  ""properties"": {
    ""rq1"": {
      ""type"": ""object"",
      ""additionalProperties"": false,
      ""properties"": {
        ""model_families"": { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [
          ""Linear/GLM"", ""Tree-based"", ""SVM"", ""kNN"", ""Naive Bayes"", ""Neural Network"",
          ""Ensemble/Stacking"", ""Probabilistic/Bayesian"", ""Rule-based/EBM"", ""Traditional clinical score""
        ]}},
        ""prediction_targets"": { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [
          ""PE"", ""early-onset PE"", ""severe PE"", ""eclampsia"", ""HELLP"", ""gestational hypertension"", ""HDP""
        ]}},
        ""prediction_timing"": { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [
          ""preconception"", ""1st trimester"", ""2nd trimester"", ""3rd trimester"", ""unspecified""
        ]}},
        ""input_modalities"": { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [
          ""EHR/clinical"", ""demographics"", ""vitals/BP"", ""laboratory"", ""biomarkers"",
          ""ultrasound/Doppler"", ""imaging"", ""genomics/omics"", ""wearables"", ""questionnaires""
        ]}},
        ""study_design"": { ""type"": ""string"", ""enum"": [""retrospective"", ""prospective"", ""cross-sectional"", ""case-control"", ""RCT"", ""NR""]},
        ""setting_scope"": { ""type"": ""string"", ""enum"": [""single-center"", ""multicenter"", ""registry"", ""other"", ""NR""]},
        ""setting_country"": { ""type"": [""string"", ""null""]},
        ""sample_size_n"": { ""type"": [""integer"", ""null""] }
      },
      ""required"": [
        ""model_families"",
        ""prediction_targets"",
        ""prediction_timing"",
        ""input_modalities"",
        ""study_design"",
        ""setting_scope"",
        ""setting_country"",
        ""sample_size_n""
      ]
    },
    ""rq2"": {
      ""type"": ""object"",
      ""additionalProperties"": false,
      ""properties"": {
        ""reported_metrics"": { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [
          ""AUC"", ""accuracy"", ""sensitivity"", ""specificity"", ""precision/PPV"", ""NPV"", ""F1"", ""calibration/Brier/HL"", ""95% CI""
        ]}},
        ""auc"": { ""type"": [""number"", ""null""]},
        ""auc_ci_low"": { ""type"": [""number"", ""null""]},
        ""auc_ci_high"": { ""type"": [""number"", ""null""]},
        ""accuracy"": { ""type"": [""number"", ""null""]},
        ""sensitivity"": { ""type"": [""number"", ""null""]},
        ""specificity"": { ""type"": [""number"", ""null""]},
        ""ppv"": { ""type"": [""number"", ""null""]},
        ""npv"": { ""type"": [""number"", ""null""]},
        ""f1"": { ""type"": [""number"", ""null""]},
        ""brier"": { ""type"": [""number"", ""null""]},
        ""hl_p"": { ""type"": [""number"", ""null""]},
        ""internal_validation"": { ""type"": ""string"", ""enum"": [""k-fold CV"", ""train/test split"", ""bootstrap"", ""none/NR""]},
        ""comparator_present"": { ""type"": [""boolean"", ""null""]},
        ""comparator_type"": { ""type"": [""string"", ""null""] }
      },
      ""required"": [
        ""reported_metrics"",
        ""auc"",
        ""auc_ci_low"",
        ""auc_ci_high"",
        ""accuracy"",
        ""sensitivity"",
        ""specificity"",
        ""ppv"",
        ""npv"",
        ""f1"",
        ""brier"",
        ""hl_p"",
        ""internal_validation"",
        ""comparator_present"",
        ""comparator_type""
      ]
    },
    ""rq3"": {
      ""type"": ""object"",
      ""additionalProperties"": false,
      ""properties"": {
        ""repository_mentioned"": { ""type"": [""boolean"", ""null""]},
        ""repository_name"": { ""type"": [""string"", ""null""]},
        ""access_type"": { ""type"": ""string"", ""enum"": [""public"", ""restricted"", ""private/on request"", ""NR""]},
        ""data_types"": { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [
          ""clinical"", ""genomics/omics"", ""imaging"", ""ultrasound/Doppler"", ""biomarkers"", ""wearables""
        ]}}
      },
      ""required"": [
        ""repository_mentioned"",
        ""repository_name"",
        ""access_type"",
        ""data_types""
      ]
    },
    ""rq4"": {
      ""type"": ""object"",
      ""additionalProperties"": false,
      ""properties"": {
        ""feature_groups"": { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [
          ""demographics"", ""obstetric history"", ""vitals (BP/MAP)"", ""labs"", ""biomarkers (PlGF/sFlt-1)"", ""ultrasound/Doppler"",
          ""comorbidities"", ""medications (aspirin)"", ""lifestyle"", ""genetics/omics"", ""others""
        ]}},
        ""top_predictors"": { ""type"": [""string"", ""null""] }
      },
      ""required"": [
        ""feature_groups"",
        ""top_predictors""
      ]
    },
    ""rq5"": {
      ""type"": ""object"",
      ""additionalProperties"": false,
      ""properties"": {
        ""real_world_design"": { ""type"": ""string"", ""enum"": [""prospective cohort"", ""observational"", ""clinical deployment"", ""NR""]},
        ""real_world_auc"": { ""type"": [""number"", ""null""]},
        ""real_world_auc_ci_low"": { ""type"": [""number"", ""null""]},
        ""real_world_auc_ci_high"": { ""type"": [""number"", ""null""]},
        ""bias_risk_signals"": { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [
          ""selection bias"", ""missing data"", ""overfitting"", ""generalizability"", ""limitations""
        ]}}
      },
      ""required"": [
        ""real_world_design"",
        ""real_world_auc"",
        ""real_world_auc_ci_low"",
        ""real_world_auc_ci_high"",
        ""bias_risk_signals""
      ]
    },
    ""rq6"": {
      ""type"": ""object"",
      ""additionalProperties"": false,
      ""properties"": {
        ""external_validation"": { ""type"": [""boolean"", ""null""]},
        ""external_validation_site"": { ""type"": [""string"", ""null""]},
        ""techniques_compared"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }},
        ""statistical_comparison"": { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [
          ""p-value"", ""DeLong"", ""NRI"", ""IDI"", ""decision curve""
        ]}}
      },
      ""required"": [
        ""external_validation"",
        ""external_validation_site"",
        ""techniques_compared"",
        ""statistical_comparison""
      ]
    },
    ""rq7"": {
      ""type"": ""object"",
      ""additionalProperties"": false,
      ""properties"": {
        ""explainability_used"": { ""type"": [""boolean"", ""null""]},
        ""explainability_methods"": { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [
          ""SHAP"", ""LIME"", ""EBM"", ""feature importance"", ""PDP/ICE"", ""attention"", ""other""
        ]}},
        ""clinical_use_of_explanations"": { ""type"": [""string"", ""null""] }
      },
      ""required"": [
        ""explainability_used"",
        ""explainability_methods"",
        ""clinical_use_of_explanations""
      ]
    },
    ""audit_note"": { ""type"": [""string"", ""null""] }
  },
  ""required"": [
    ""rq1"", ""rq2"", ""rq3"", ""rq4"", ""rq5"", ""rq6"", ""rq7"", ""audit_note""
  ]
}";



        // Método principal solicitado
        private static void chatGPT_Process_Run()
        {
            // Config OpenAI            
            string apiKey = "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";

            // Cliente del API (lib oficial)
            var chatClient = new ChatClient(model: "gpt-5", apiKey: apiKey); // puedes cambiar modelo

            using (DOC_TmpEntities db = new DOC_TmpEntities())
            {
                db.Configuration.AutoDetectChangesEnabled = true;

                var pending = db.BibEntry
                                .Where(b => b.EntryStatus == 1)
                                .OrderBy(b => b.EntryID)
                                .ToList();

                foreach (var ent in pending)
                {
                    // Construir contexto del artículo (solo campos permitidos)
                    string title = Safe(ent.title, 7900);
                    string abstr = Safe(ent.@abstract, 7900);
                    string year = Safe(ent.year, 20);
                    string doi = Safe(ent.doi, 255);

                    var sbUser = new StringBuilder();
                    sbUser.AppendLine("You are coding a systematic review for preeclampsia prediction models.");
                    sbUser.AppendLine("Use ONLY the following fields to extract information: title, abstract, year, doi.");
                    sbUser.AppendLine("If a value is not explicitly supported by these fields, return null or an empty array.");
                    sbUser.AppendLine("Do NOT infer from outside knowledge. Do NOT guess.");
                    sbUser.AppendLine();
                    sbUser.AppendLine("ARTICLE DATA:");
                    sbUser.AppendLine($"Title: {title}");
                    sbUser.AppendLine($"Abstract: {abstr}");
                    sbUser.AppendLine($"Year: {year}");
                    sbUser.AppendLine($"DOI: {doi}");
                    sbUser.AppendLine();
                    sbUser.AppendLine("Return a single JSON object following the exact JSON Schema.");

                    // Mensajes del chat
                    var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a careful research assistant. Extract contestable information only from the provided title/abstract/year/doi for a preeclampsia ML/AI review."),
                    new UserChatMessage(sbUser.ToString())
                };

                    // Options con Structured Outputs (JSON Schema estricto)
                    var options = new ChatCompletionOptions
                    {
                        Temperature = 1,
                        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                            jsonSchemaFormatName: "preeclampsia_review_schema",
                            jsonSchema: System.BinaryData.FromString(_jsonSchema),
                            jsonSchemaIsStrict: true
                        )
                    };

                    // Llamada con reintentos por robustez
                    string rawJson = null;
                    Root parsed = null;

                    TryWithExponentialBackoff(() =>
                    {
                        var completion = chatClient.CompleteChat(messages, options);
                        rawJson = completion?.Value.Content?.FirstOrDefault()?.Text;
                        if (string.IsNullOrWhiteSpace(rawJson))
                            throw new InvalidOperationException("Respuesta vacía del modelo.");

                        parsed = JsonConvert.DeserializeObject<Root>(rawJson);
                    });

                    // Mapeo a columnas, respetando nulos y longitudes
                    if (parsed != null)
                    {
                        // === RQ1 ===
                        ent.rq1_model_families_json = ToJsonOrNull(parsed.rq1?.model_families);
                        ent.rq1_prediction_targets_json = ToJsonOrNull(parsed.rq1?.prediction_targets);
                        ent.rq1_prediction_timing_json = ToJsonOrNull(parsed.rq1?.prediction_timing);
                        ent.rq1_input_modalities_json = ToJsonOrNull(parsed.rq1?.input_modalities);
                        ent.rq1_study_design = Trunc(parsed.rq1?.study_design, 50);
                        ent.rq1_setting_scope = Trunc(parsed.rq1?.setting_scope, 50);
                        ent.rq1_setting_country = Trunc(parsed.rq1?.setting_country, 200);
                        ent.rq1_sample_size_n = parsed.rq1?.sample_size_n;

                        // === RQ2 ===
                        ent.rq2_reported_metrics_json = ToJsonOrNull(parsed.rq2?.reported_metrics);
                        ent.rq2_auc = ToDec(parsed.rq2?.auc, 3);
                        ent.rq2_auc_ci_low = ToDec(parsed.rq2?.auc_ci_low, 3);
                        ent.rq2_auc_ci_high = ToDec(parsed.rq2?.auc_ci_high, 3);
                        ent.rq2_accuracy = ToDec(parsed.rq2?.accuracy, 3);
                        ent.rq2_sensitivity = ToDec(parsed.rq2?.sensitivity, 3);
                        ent.rq2_specificity = ToDec(parsed.rq2?.specificity, 3);
                        ent.rq2_ppv = ToDec(parsed.rq2?.ppv, 3);
                        ent.rq2_npv = ToDec(parsed.rq2?.npv, 3);
                        ent.rq2_f1 = ToDec(parsed.rq2?.f1, 3);
                        ent.rq2_brier = ToDec(parsed.rq2?.brier, 4);
                        ent.rq2_hl_p = ToDec(parsed.rq2?.hl_p, 4);
                        ent.rq2_internal_validation = Trunc(parsed.rq2?.internal_validation, 50);
                        ent.rq2_comparator_present = parsed.rq2?.comparator_present;
                        ent.rq2_comparator_type = Trunc(parsed.rq2?.comparator_type, 100);

                        // === RQ3 ===
                        ent.rq3_repository_mentioned = parsed.rq3?.repository_mentioned;
                        ent.rq3_repository_name = Trunc(parsed.rq3?.repository_name, 200);
                        ent.rq3_access_type = Trunc(parsed.rq3?.access_type, 30);
                        ent.rq3_data_types_json = ToJsonOrNull(parsed.rq3?.data_types);

                        // === RQ4 ===
                        ent.rq4_feature_groups_json = ToJsonOrNull(parsed.rq4?.feature_groups);
                        ent.rq4_top_predictors = parsed.rq4?.top_predictors;

                        // === RQ5 ===
                        ent.rq5_real_world_design = Trunc(parsed.rq5?.real_world_design, 50);
                        ent.rq5_real_world_auc = ToDec(parsed.rq5?.real_world_auc, 3);
                        ent.rq5_real_world_auc_ci_low = ToDec(parsed.rq5?.real_world_auc_ci_low, 3);
                        ent.rq5_real_world_auc_ci_high = ToDec(parsed.rq5?.real_world_auc_ci_high, 3);
                        ent.rq5_bias_risk_signals_json = ToJsonOrNull(parsed.rq5?.bias_risk_signals);

                        // === RQ6 ===
                        ent.rq6_external_validation = parsed.rq6?.external_validation;
                        ent.rq6_external_validation_site = Trunc(parsed.rq6?.external_validation_site, 200);
                        ent.rq6_techniques_compared_json = ToJsonOrNull(parsed.rq6?.techniques_compared);
                        ent.rq6_statistical_comparison_json = ToJsonOrNull(parsed.rq6?.statistical_comparison);

                        // === RQ7 ===
                        ent.rq7_explainability_used = parsed.rq7?.explainability_used;
                        ent.rq7_explainability_methods_json = ToJsonOrNull(parsed.rq7?.explainability_methods);
                        ent.rq7_clinical_use_of_explanations = Trunc(parsed.rq7?.clinical_use_of_explanations, 400);

                        // Nota y JSON crudo (auditoría)
                        string audit = BuildAuditNote(parsed.audit_note, title, year, doi);
                        ent.note = Trunc(audit, 1000);

                        // rq1_json es VARCHAR(4000): truncamos si es necesario
                        ent.rq1_json = Trunc(rawJson, 4000);
                    }

                    // Guardado por registro
                    db.SaveChanges();
                }
            }
        }

        // === Utilidades ===

        private static string Safe(string s, int max) => string.IsNullOrEmpty(s) ? "" : (s.Length > max ? s.Substring(0, max) : s);

        private static string Trunc(string s, int max)
            => string.IsNullOrEmpty(s) ? null : (s.Length > max ? s.Substring(0, max) : s);

        private static string ToJsonOrNull(IEnumerable<string> items)
        {
            if (items == null) return null;
            var list = items.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            if (list.Count == 0) return null;
            return JsonConvert.SerializeObject(list);
        }

        private static decimal? ToDec(decimal? val, int scale)
        {
            if (val == null) return null;
            // Redondeo seguro a la escala de la columna
            return Math.Round(val.Value, scale, MidpointRounding.AwayFromZero);
        }

        private static string BuildAuditNote(string modelNote, string title, string year, string doi)
        {
            var sb = new StringBuilder();
            sb.Append("auto-coded from title/abstract");
            if (!string.IsNullOrWhiteSpace(year)) sb.Append($"; year={year}");
            if (!string.IsNullOrWhiteSpace(doi)) sb.Append($"; doi={doi}");
            if (!string.IsNullOrWhiteSpace(modelNote)) sb.Append($"; note=" + modelNote);
            sb.Append($"; ts={DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
            return sb.ToString();
        }

        private static void TryWithExponentialBackoff(Action action, int maxAttempts = 5, int baseDelayMs = 1000)
        {
            int attempt = 0;
            Exception last = null;
            var rnd = new Random();
            while (attempt < maxAttempts)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    last = ex;
                    attempt++;
                    int delay = baseDelayMs * (int)Math.Pow(2, attempt - 1) + rnd.Next(0, 400);
                    System.Threading.Thread.Sleep(delay);
                }
            }
            throw new InvalidOperationException("Fallo persistente al llamar al modelo.", last);
        }

    }

    public class Choice
    {
        public string text { get; set; }
        public int index { get; set; }
        public object logprobs { get; set; }
        public string finish_reason { get; set; }
    }
    public class Root
    {
        public string id { get; set; }
        public string @object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public List<Choice> choices { get; set; }
        public Usage usage { get; set; }
    }
    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }
    public class OpenAIChoice
    {
        public string text { get; set; }
        public float probability { get; set; }
        public float[] logprobs { get; set; }
        public int[] finish_reason { get; set; }
    }
    public class OpenAIRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("temperature")]
        public float Temperature { get; set; }

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; }
    }

    public class OpenAIChatRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("temperature")]
        public float Temperature { get; set; }

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonProperty("messages")]
        public List<Message> messages { get; set; }
    }
    public class OpenAIErrorResponse
    {
        [JsonProperty("error")]
        public OpenAIError Error { get; set; }
    }
    public class OpenAIError
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("param")]
        public string Param { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }

    public class OpenAIChatResponse
    {
        public string id { get; set; }

        [JsonProperty("object")]
        public string objeto { get; set; }

        public string created { get; set; }

        public string model { get; set; }

        public OpenAIChatResponseUsage usage { get; set; }

        public List<OpenAIChatResponseChoice> choices { get; set; }
    }

    public class OpenAIChatResponseUsage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }

    }

    public class OpenAIChatResponseChoice
    {
        public int index { get; set; }
        public string finish_reason { get; set; }

        public OpenAIChatResponseChoiceMessage message { get; set; }
    }

    public class OpenAIChatResponseChoiceMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class ProximityGPT
    {
        public float proximity { get; set; }
        public string reason { get; set; }
    }
}
