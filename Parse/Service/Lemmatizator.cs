using DeepMorphy;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Service
{
    class Lemmatizator
    {
        MorphAnalyzer morph;

        Dictionary<string, string> lemms;

        public Lemmatizator()
        {
            morph = new MorphAnalyzer(withLemmatization: true);
            lemms = FileManager.OpenLemms();
            AppDomain.CurrentDomain.ProcessExit += ProcessExitHandler;
        }

        private void ProcessExitHandler(object sender, EventArgs e)
        {
            Save();
        }

        public string GetLemma(string word)
        {
            string lemma = null;
            if (lemms.TryGetValue(word.ToLower(), out lemma))
            {
                return lemma;
            }
            try
            {
                var lemmword = morph.Parse(word.ToLower()).ToArray();
                lemma = lemmword[0].BestTag.Lemma;
            }
            catch (Exception ex)
            {

            }

            if (lemma is not null)
            {
                if (lemms.Keys.Count % 1000 == 0)
                    Console.WriteLine($"Ключей в словаре: $ {lemms.Keys.Count()}");
                var sortedKeys = lemms.Keys
                    .Where(k => k.Length >= 4 && k.Length <= word.Length - 2)
                    .OrderBy(k => k.Length);

                foreach (var key in sortedKeys)
                {
                    if (word.StartsWith(key))
                    {
                        try
                        {
                            lemms.Add(word, lemms[key]);
                        }
                        catch (Exception ex)
                        {
                        }
                        return lemms[key];
                    }
                }
                try
                {
                    lemms.Add(word, lemma);
                }
                catch (Exception ex)
                {

                }
                return lemma;
            }
            return " ";
        }

        public void Save()
        {
            FileManager.SaveLemms(lemms);
        }
    }
}
