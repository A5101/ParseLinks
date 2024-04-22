using DeepMorphy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            morph = new MorphAnalyzer(withLemmatization:true);
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
            if (lemms.TryGetValue(word, out lemma))
            {
                return lemma;
            }
            try
            {
                var lemmword = morph.Parse(word).ToArray();
                lemma = lemmword[0].BestTag.Lemma;
            }
            catch (Exception ex)
            {
                int i = 0;
            }

            if (lemma is not null)
            {
                lemms.Add(word, lemma);
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
