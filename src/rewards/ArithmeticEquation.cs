using System;
using GrimDynasty.TokenActions;
namespace GrimDynasty.Rewards {
 // A proved subset of the shipped parser: fully parenthesized +,-,*,/ trees,
 // exact nonnegative integer literals and the ASCII playerLevel resolver.
 public sealed class ArithmeticEquation {
  sealed class Node {
   internal readonly char Op;internal readonly float Literal;internal readonly Node Left,Right;
   internal Node(char op,float literal,Node left,Node right){Op=op;Literal=literal;Left=left;Right=right;}
   internal float Evaluate(float input){
    if(Op=='n')return Literal;if(Op=='v')return input;
    float a=Left.Evaluate(input),b=Right.Evaluate(input),r;
    switch(Op){case '+':r=a+b;break;case '-':r=a-b;break;case '*':r=a*b;break;case '/':r=a/b;break;default:throw new NotSupportedException("UnknownArithmeticNode");}
    r=BitConverter.ToSingle(BitConverter.GetBytes(r),0);
    if(float.IsNaN(r)||float.IsInfinity(r)||(r!=0&&Math.Abs(r)<1.17549435E-38f))throw new NotSupportedException("NonfiniteOrSubnormalArithmeticDomain");return r;
   }
  }
  sealed class Parser {
   readonly string text;int at;
   internal Parser(string text){this.text=text;}
   void Spaces(){while(at<text.Length&&text[at]==' ')at++;}
   static bool Letter(char c){return c>='a'&&c<='z'||c>='A'&&c<='Z';}
   Node Expression(int depth){
    if(depth>64)throw new NotSupportedException("EquationDepthOutsideCoveredDomain");Spaces();if(at>=text.Length)throw new NotSupportedException("IncompleteEquation");char ch=text[at];
    if(ch>='0'&&ch<='9'){
     uint v=0;do{v=v*10u+(uint)(text[at]-'0');if(v>16777216)throw new NotSupportedException("LiteralOutsideExactIntegerDomain");at++;}while(at<text.Length&&text[at]>='0'&&text[at]<='9');
     if(at<text.Length&&text[at]=='.')throw new NotSupportedException("DecimalAtOfConversionNotRecovered");return new Node('n',v,null,null);
    }
    if(Letter(ch)){int start=at++;while(at<text.Length&&Letter(text[at]))at++;string name=text.Substring(start,at-start);if(!name.Equals("playerLevel",StringComparison.OrdinalIgnoreCase))throw new NotSupportedException("EquationResolverVariableNotRecovered");return new Node('v',0,null,null);}
    if(ch!='(')throw new NotSupportedException("EquationTokenOutsideCoveredGrammar");at++;Node left=Expression(depth+1);Spaces();if(at<text.Length&&text[at]==')'){at++;return left;}
    if(at>=text.Length)throw new NotSupportedException("IncompleteEquation");char op=text[at++];if(op!='+'&&op!='-'&&op!='*'&&op!='/')throw new NotSupportedException(op=='^'?"NativePowfNotRecovered":"EquationOperatorOutsideCoveredDomain");Node right=Expression(depth+1);Spaces();if(at>=text.Length||text[at++]!=')')throw new NotSupportedException("FullyParenthesizedEquationRequired");return new Node(op,0,left,right);
   }
   internal Node Read(){Node n=Expression(0);Spaces();if(at!=text.Length)throw new NotSupportedException("FullyParenthesizedEquationRequired");return n;}
  }
  readonly Node root;
  ArithmeticEquation(Node root){this.root=root;}
  public static MutationResult Compile(string source,out ArithmeticEquation equation){
   equation=null;if(source==null)return MutationResult.Unsupported("UnknownEquationSource");int zero=source.IndexOf('\0');if(zero>=0)source=source.Substring(0,zero);if(source.Length>2048)return MutationResult.Unsupported("EquationLengthOutsideCoveredDomain");
   try{equation=new ArithmeticEquation(new Parser(source).Read());return MutationResult.Known(false);}catch(NotSupportedException e){return MutationResult.Unsupported(e.Message);}
  }
  public MutationResult Evaluate(int storedInput,out float value){
   value=0;uint input=unchecked((uint)storedInput);if(input>16777216)return MutationResult.Unsupported("PlayerLevelOutsideExactFloatIntegerDomain");
   try{value=root.Evaluate(input);return MutationResult.Known(false);}catch(NotSupportedException e){return MutationResult.Unsupported(e.Message);}
  }
 }
}
