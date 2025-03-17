using E.Standard.WebMapping.Core;
using System;

namespace WebGIS.Tests;

internal class Evaluator
{
    public void Test()
    {
        Console.WriteLine("$eval and $round");
        Console.WriteLine(Eval.ParseEvalExpression("$eval(1000/55)"));
        Console.WriteLine(Eval.ParseEvalExpression("$round5($eval(1000/55))"));
        Console.WriteLine(Eval.ParseEvalExpression("$round4($eval(1000/55))"));
        Console.WriteLine(Eval.ParseEvalExpression("$round3($eval(1000/55))"));
        Console.WriteLine(Eval.ParseEvalExpression("$round2($eval(1000/55))"));
        Console.WriteLine(Eval.ParseEvalExpression("$round1($eval(1000/55))"));
        Console.WriteLine(Eval.ParseEvalExpression("$round0($eval(1000/55))"));
        Console.WriteLine(Eval.ParseEvalExpression("$round5($eval(1000/55*$pi()))"));
        Console.WriteLine("-------------------------------------------------");

        Console.WriteLine("$round");
        Console.WriteLine(Eval.ParseEvalExpression("$round5(1000.12)"));
        Console.WriteLine(Eval.ParseEvalExpression("$round4(1000.12)"));
        Console.WriteLine(Eval.ParseEvalExpression("$round3(1000.12)"));
        Console.WriteLine(Eval.ParseEvalExpression("$round2(1000.12)"));
        Console.WriteLine(Eval.ParseEvalExpression("$round1(1000.12)"));
        Console.WriteLine(Eval.ParseEvalExpression("$round0(1000.12)"));
        Console.WriteLine("-------------------------------------------------");

        Console.WriteLine("Trigonometry");
        Console.WriteLine(Eval.ParseEvalExpression("sin(0.5):  $sin(.5)"));
        Console.WriteLine(Eval.ParseEvalExpression("cos(0.5):  $cos(.5)"));
        Console.WriteLine(Eval.ParseEvalExpression("tan(0.5):  $tan(.5)"));
        Console.WriteLine("-------------------------------------------------");

        Console.WriteLine("Format 1000,1234567");
        Console.WriteLine(Eval.ParseEvalExpression("$n0(1000,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n1(1000,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n2(1000,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n3(1000,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n4(1000,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n5(1000,1234567)"));

        Console.WriteLine("Format 1000,1234567 (de)");
        Console.WriteLine(Eval.ParseEvalExpression("$n0_de(1000,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n1_de(1000,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n2_de(1000,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n3_de(1000,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n4_de(1000,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n5_de(1000,1234567)"));

        Console.WriteLine("Format 1000,12 (de)");
        Console.WriteLine(Eval.ParseEvalExpression("$n0_de(1000,12)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n1_de(1000,12)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n2_de(1000,12)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n3_de(1000,12)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n4_de(1000,12)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n5_de(1000,12)"));

        Console.WriteLine("Invalid numbers...");
        Console.WriteLine(Eval.ParseEvalExpression("$round2(1000a,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$n1_de(1000a,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$sin(a,1234567)"));
        Console.WriteLine(Eval.ParseEvalExpression("$eval(1000a,1234567/8)"));
    }
}
