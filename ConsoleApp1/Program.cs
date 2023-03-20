// See https://aka.ms/new-console-template for more information

using DataCollector;
using DataCollector.Extractor;
using DataCollector.Extractor.Factorry;

var ext = new ExtractorDirector("https://help.bigtime.net/hc/en-us");
if(await ext.ExtractAsync())
{
    await ext.SaveToFile(@"C:\Users\file.csv");
}