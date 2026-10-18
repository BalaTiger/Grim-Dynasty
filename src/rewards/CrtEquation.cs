using System;
using System.Collections.Generic;
using GrimDynasty.TokenActions;
namespace GrimDynasty.Rewards {
 public sealed class CrtEquation {
  sealed class Node {
   internal readonly char Op;internal readonly float Value;internal readonly Node Left,Right;
   internal Node(char op,float value,Node left,Node right){Op=op;Value=value;Left=left;Right=right;}
   internal bool Constant{get{return Op=='n';}}
   internal float Eval(uint input,SystemCrtMath math){
    if(Op=='n')return Value;if(Op=='v')return input;
    float a,b,r;
    if(Op=='~'){r=Left.Eval(input,math);return BitConverter.ToSingle(BitConverter.GetBytes(BitConverter.ToUInt32(BitConverter.GetBytes(r),0)^0x80000000u),0);}
    if(Op=='^'){b=Right.Eval(input,math);a=Left.Eval(input,math);var q=math.Power(a,b,out r);if(q.Status!="Known")throw new NotSupportedException(q.Reason);}
    else{a=Left.Eval(input,math);b=Right.Eval(input,math);switch(Op){case '+':r=a+b;break;case '-':r=a-b;break;case '*':r=a*b;break;case '/':r=a/b;break;default:throw new NotSupportedException("UnknownCrtEquationNode");}}
    r=BitConverter.ToSingle(BitConverter.GetBytes(r),0);Normal(r);return r;
   }
  }
  readonly Node root;readonly SystemCrtMath math;
  CrtEquation(Node root,SystemCrtMath math){this.root=root;this.math=math;}
  static void Normal(float value){if(float.IsNaN(value)||float.IsInfinity(value)||(value!=0&&Math.Abs(value)<1.17549435E-38f))throw new NotSupportedException("EquationNonfiniteOrSubnormalOutsideCoveredDomain");}
  static int Priority(char op){return op=='^'?3:op=='~'?2:op=='*'||op=='/'?1:0;}
  static bool Letter(char c){return c>='a'&&c<='z'||c>='A'&&c<='Z';}
  static void Reduce(Stack<Node> values,char op,SystemCrtMath math){
   int needed=op=='~'?1:2;if(values.Count<needed)throw new NotSupportedException("NativeEquationOperandUnderflow");Node right=op=='~'?null:values.Pop(),left=values.Pop();var n=new Node(op,0,left,right);
   if(left.Constant&&(right==null||right.Constant))n=new Node('n',n.Eval(0,math),null,null);values.Push(n);
  }
  public static MutationResult Compile(SystemCrtMath math,string source,out CrtEquation equation){
   equation=null;if(math==null||source==null)return MutationResult.Unsupported("UnknownCrtEquationSourceOrMath");uint control;var ready=math.Check(out control);if(ready.Status!="Known")return ready;
   int zero=source.IndexOf('\0');if(zero>=0)source=source.Substring(0,zero);if(source.Length>2048)return MutationResult.Unsupported("EquationLengthOutsideCoveredDomain");
   var values=new Stack<Node>();var operators=new Stack<char>();bool operand=true;int at=0;
   try{while(at<source.Length){char c=source[at];
     if(c==' '){at++;continue;}
     if(c=='.'||c>='0'&&c<='9'){int start=at++;while(at<source.Length&&(source[at]=='.'||source[at]>='0'&&source[at]<='9'))at++;float value;var parsed=math.Literal(source.Substring(start,at-start),out value);if(parsed.Status!="Known")return parsed;Normal(value);values.Push(new Node('n',value,null,null));operand=false;continue;}
     if(Letter(c)){int start=at++;while(at<source.Length&&Letter(source[at]))at++;if(!source.Substring(start,at-start).Equals("playerLevel",StringComparison.OrdinalIgnoreCase))return MutationResult.Unsupported("CrtEquationResolverNotRecovered");values.Push(new Node('v',0,null,null));operand=false;continue;}
     if(c=='('){operators.Push(c);operand=true;at++;continue;}
     if(c==')'){if(operand)throw new NotSupportedException("NativeEquationUnexpectedClose");while(operators.Count!=0&&operators.Peek()!='(')Reduce(values,operators.Pop(),math);if(operators.Count==0)throw new NotSupportedException("NativeEquationUnmatchedClose");operators.Pop();at++;continue;}
     if(c=='+'||c=='-'||c=='*'||c=='/'||c=='^'){
      if(operand&&c=='+'){at++;continue;}char op=c;
      if(operand){if(c!='-')throw new NotSupportedException("NativeEquationUnexpectedOperator");op='~';}
      while(operators.Count!=0&&operators.Peek()!='('){char top=operators.Peek();bool pop=top=='~'?Priority(op)<Priority(top):Priority(op)<=Priority(top);if(!pop)break;Reduce(values,operators.Pop(),math);}
      operators.Push(op);operand=true;at++;continue;
     }
     throw new NotSupportedException("CrtEquationTokenOutsideNativeLexer");
    }
    while(operators.Count!=0){char op=operators.Pop();if(op=='(')throw new NotSupportedException("NativeEquationUnmatchedOpen");Reduce(values,op,math);}
    if(values.Count!=1)throw new NotSupportedException("NativeEquationResultArity");equation=new CrtEquation(values.Pop(),math);return MutationResult.Known(false);
   }catch(NotSupportedException e){return MutationResult.Unsupported(e.Message);}
  }
  public MutationResult Evaluate(int storedInput,out float value){
   value=0;uint control;var ready=math.Check(out control);if(ready.Status!="Known")return ready;
   try{value=root.Eval(unchecked((uint)storedInput),math);Normal(value);return MutationResult.Known(false);}catch(NotSupportedException e){return MutationResult.Unsupported(e.Message);}
  }
 }
}
