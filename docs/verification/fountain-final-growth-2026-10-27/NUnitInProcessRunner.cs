using System;using System.Collections.Generic;using System.IO;using System.Reflection;using NUnit.Framework.Api;using NUnit.Framework.Internal;using NUnit.Framework.Interfaces;
class Program {static int Main(string[] args) {
 var runner=new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
 runner.Load(Assembly.LoadFrom(args[0]),new Dictionary<string,object>{{"NumberOfTestWorkers",0}});
 var filter=args.Length>2?TestFilter.FromXml(args[2]):TestFilter.Empty;
 var result=runner.Run(TestListener.NULL,filter);
 File.WriteAllText(args[1],result.ToXml(true).OuterXml);
 void Show(ITestResult r){if(!r.HasChildren && r.ResultState.Status!=TestStatus.Passed)Console.WriteLine(r.FullName+" "+r.ResultState+"\n"+r.Message);foreach(var c in r.Children)Show(c);}
 Show(result);Console.WriteLine($"NUnit in-process: total={result.TotalCount}, passed={result.PassCount}, failed={result.FailCount}, skipped={result.SkipCount}, inconclusive={result.InconclusiveCount}, duration={result.Duration:F3}s");
 return result.FailCount==0 && result.SkipCount==0 && result.InconclusiveCount==0 && result.PassCount>0?0:1;
}}