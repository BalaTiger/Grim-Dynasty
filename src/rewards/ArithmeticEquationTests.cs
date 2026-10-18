using System;
using GrimDynasty.Rewards;
using GrimDynasty.TaskAuto;
class ArithmeticEquationTests {
 static int n;static void Need(bool b,string s){n++;if(!b)throw new Exception(s);}
 static float Value(string source,int input){ArithmeticEquation e;Need(ArithmeticEquation.Compile(source,out e).Status=="Known","compile "+source);float value;Need(e.Evaluate(input,out value).Status=="Known","evaluate "+source);return value;}
 static void Main(){
  Need(Value("(playerLevel*100)",2)==200,"variable multiplication");Need(Value(" ((PLAYERLEVEL*100)+20) ",3)==320,"ASCII case insensitive resolver and spaces");
  Need(Value("((10-3)/2)",0)==3.5f,"ordered subtraction and division");Need(Value("((16777216+1)-16777216)",0)==0,"single precision per-node rounding");Need(Value("(playerLevel)",5)==5,"grouping");Need(Value("(1+2)\0ignored",0)==3,"C-string termination");
  ArithmeticEquation e;Need(ArithmeticEquation.Compile("(playerLevel^2)",out e).Status=="Unsupported"&&e==null,"native powf is not substituted");Need(ArithmeticEquation.Compile("(playerLevel*1.16)",out e).Status=="Unsupported","decimal conversion bounded");Need(ArithmeticEquation.Compile("playerLevel*100",out e).Status=="Unsupported","unproved precedence excluded");Need(ArithmeticEquation.Compile("charLevel",out e).Status=="Unsupported","other resolver branch not guessed");Need(ArithmeticEquation.Compile("-2",out e).Status=="Unsupported","unary grammar not guessed");Need(ArithmeticEquation.Compile("16777217",out e).Status=="Unsupported","integer literal boundary");
  ArithmeticEquation.Compile("(playerLevel/0)",out e);float result;Need(e.Evaluate(1,out result).Status=="Unsupported","division nonfinite outside domain");ArithmeticEquation.Compile("playerLevel",out e);Need(e.Evaluate(-1,out result).Status=="Unsupported","resolver uses unsigned input");Need(e.Evaluate(16777216,out result).Status=="Known"&&result==16777216,"exact integer endpoint");
  var x=ExampleLevelUpQuest.Create();ArithmeticEquation program;Need(ArithmeticEquation.Compile("(playerLevel*100)",out program).Status=="Known","XP program");x.Player.Progression.Equation=program;x.Player.Progression.FormulaSamples.Clear();x.Player.NextThreshold=null;var a=x.Quest.Auto;AutomaticTasks.Start(a);AutomaticTasks.Update(a);x.Quest.Kill();AutomaticTasks.Update(a);x.Quest.Kill();Need(AutomaticTasks.Update(a).Status=="Known"&&x.Player.Level1760==2&&x.Player.Experience1764==125,"parsed program drives real level-up without threshold samples");Need(x.Player.Progression.SkillPoints176C==3&&x.Completion.World.LevelDisplay.Queue.Messages.Count==1,"formula to points and cinematic closure");
  Console.WriteLine("Arithmetic equation assertions passed: "+n);
 }
}
