using System;
using System.Collections.Generic;
using System.IO;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.pipeline;
using edu.stanford.nlp.util;
using java.util;

public class Lemmatizer
{
    private readonly StanfordCoreNLP _pipeline;

    public Lemmatizer()
    {
        // Set up properties for the Stanford NLP pipeline
        var props = new Properties();
        props.setProperty("annotators", "tokenize,ssplit,pos,lemma");
        props.setProperty("tokenize.language", "en");

        // Initialize the pipeline with the properties
        _pipeline = new StanfordCoreNLP(props);
    }

    public List<string> Lemmatize(string text)
    {
        var lemmas = new List<string>();

        // Create a document and annotate it
        var document = new Annotation(text);
        _pipeline.annotate(document);

        // Extract the lemmatized tokens
        var sentences = document.get(new CoreAnnotations.SentencesAnnotation().getClass()) as java.util.ArrayList;
        foreach (CoreMap sentence in sentences)
        {
            var tokens = sentence.get(new CoreAnnotations.TokensAnnotation().getClass()) as java.util.ArrayList;
            foreach (CoreLabel token in tokens)
            {
                var lemma = token.get(new CoreAnnotations.LemmaAnnotation().getClass());
                lemmas.Add(lemma.ToString());
            }
        }

        return lemmas;
    }
}
