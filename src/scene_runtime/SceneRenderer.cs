using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace GrimDynasty.Scene {
 // Original project art and camera adapter. No original game asset or renderer
 // is used; drawing the scene does not establish native rendering compatibility.
 public static class SceneRenderer {
  static readonly Color Ground=Color.FromArgb(103,106,72);
  static readonly Font LabelFont=MakeFont(10.5f,FontStyle.Regular);
  static readonly Font SmallFont=MakeFont(8.5f,FontStyle.Regular);
  static readonly Font PlaceFont=MakeFont(17,FontStyle.Bold);
  static Font MakeFont(float size,FontStyle style){
   try{return new Font("Microsoft YaHei",size,style,GraphicsUnit.Pixel);}
   catch(ArgumentException){return new Font(FontFamily.GenericSansSerif,size,style,GraphicsUnit.Pixel);}
  }

  public static PointF Camera(SceneWorld world,int viewportW,int viewportH){
   if(world==null)throw new ArgumentNullException("world");
   return new PointF(Math.Max(0,Math.Min(SceneWorld.WorldWidth-viewportW,world.PlayerX-viewportW/2f)),
    Math.Max(0,Math.Min(SceneWorld.WorldHeight-viewportH,world.PlayerY-viewportH/2f)));
  }

  public static void Draw(Graphics g,SceneWorld world,int viewportW,int viewportH){
   DrawScene(g,world,new SceneAgentSnapshot[0],viewportW,viewportH);
  }
  public static void Draw(Graphics g,SceneState scene,int viewportW,int viewportH){
   if(scene==null)throw new ArgumentNullException("scene");
   DrawScene(g,scene.World,scene.Agents.GetSnapshots(),viewportW,viewportH);
  }
  static void DrawScene(Graphics g,SceneWorld world,SceneAgentSnapshot[] agents,int viewportW,int viewportH){
   if(g==null||world==null)throw new ArgumentNullException(g==null?"g":"world");
   if(viewportW<1||viewportH<1)return;
   var state=g.Save();
   try {
    g.SetClip(new Rectangle(0,0,viewportW,viewportH),CombineMode.Intersect);
    g.SmoothingMode=SmoothingMode.AntiAlias;
    g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
    using(var brush=new SolidBrush(Color.FromArgb(28,34,29)))g.FillRectangle(brush,0,0,viewportW,viewportH);
    PointF camera=Camera(world,viewportW,viewportH);
    g.TranslateTransform(-camera.X,-camera.Y);
    var visible=new RectangleF(camera.X-90,camera.Y-100,viewportW+180,viewportH+200);
    using(var brush=new SolidBrush(Ground))g.FillRectangle(brush,0,0,SceneWorld.WorldWidth,SceneWorld.WorldHeight);
    Terrain(g,visible);
    Road(g,world.RoadPoints);
    Courtyard(g,new RectangleF(126,728,650,678),visible);
    Grass(g,visible);
    Landmarks(g);
    foreach(var rock in world.Rocks)if(visible.IntersectsWith(rock))Rock(g,rock);
    // Geometry is compact. Y ordering gives the player and NPCs a consistent
    // foreground/background relationship with buildings and trees.
    var objects=new System.Collections.Generic.List<DrawItem>();
    foreach(var building in world.Structures)if(visible.IntersectsWith(Expanded(building.Bounds,45)))objects.Add(new DrawItem{Y=building.Bounds.Bottom,Structure=building});
    foreach(var tree in world.Trees)if(visible.Contains(tree))objects.Add(new DrawItem{Y=tree.Y+7,Tree=tree,Kind=1});
    foreach(var npc in world.Npcs)objects.Add(new DrawItem{Y=npc.Y+8,Npc=npc,Kind=2});
    foreach(var agent in agents)objects.Add(new DrawItem{Y=agent.Y+8,Agent=agent,Kind=4});
    objects.Add(new DrawItem{Y=world.PlayerY+8,Kind=3});
    objects.Sort(delegate(DrawItem a,DrawItem b){return a.Y.CompareTo(b.Y);});
    foreach(var item in objects){
     if(item.Kind==1)Tree(g,item.Tree);
     else if(item.Kind==2)Npc(g,item.Npc,world);
     else if(item.Kind==3)Player(g,world);
     else if(item.Kind==4)Agent(g,item.Agent);
     else Structure(g,item.Structure);
    }
    // A small destination marker belongs to the world, not to a replacement UI.
    Caption(g,"辽西驿堡",450,690,PlaceFont,Color.FromArgb(232,222,183));
    Caption(g,"残烽台",1830,120,PlaceFont,Color.FromArgb(224,213,181));
    Caption(g,"北驿道",1248,768,LabelFont,Color.FromArgb(231,219,177));
   } finally {g.Restore(state);}
  }

  sealed class DrawItem {public float Y;public int Kind;public SceneStructure Structure;public SceneNpc Npc;public SceneAgentSnapshot Agent;public PointF Tree;}
  static RectangleF Expanded(RectangleF r,float amount){r.Inflate(amount,amount);return r;}
  static int Hash(int x,int y){unchecked {uint h=(uint)(x*374761393+y*668265263);h=(h^(h>>13))*1274126177;return (int)(h&0x7fffffff);}}
  static void Terrain(Graphics g,RectangleF visible){
   using(var dull=new SolidBrush(Color.FromArgb(114,112,77)))g.FillEllipse(dull,900,1120,1100,650);
   using(var high=new SolidBrush(Color.FromArgb(122,118,82)))g.FillEllipse(high,1460,80,670,620);
   using(var low=new SolidBrush(Color.FromArgb(83,100,72)))g.FillEllipse(low,-170,-170,1230,850);
   using(var patch=new SolidBrush(Color.FromArgb(60,122,119,81))){
    for(int y=(int)visible.Top/160*160;y<visible.Bottom;y+=160)
     for(int x=(int)visible.Left/160*160;x<visible.Right;x+=160){int h=Hash(x,y);g.FillEllipse(patch,x+(h%50),y+(h%43),100+h%130,50+h%90);}
   }
   // Narrow furrows and dry crop stems outside the palisade are decoration only.
   using(var furrow=new Pen(Color.FromArgb(92,88,60),3))
    for(int y=1240;y<1490;y+=24)g.DrawLine(furrow,1010,y,1370,y-68);
   using(var wheat=new Pen(Color.FromArgb(171,151,90),1.3f))
    for(int x=1030;x<1370;x+=23)for(int y=1250;y<1450;y+=31){g.DrawLine(wheat,x,y,x+1,y-10);g.DrawLine(wheat,x-4,y-8,x+1,y-4);}
  }
  static void Road(Graphics g,PointF[] points){
   if(points==null||points.Length<2)return;
   using(var path=new GraphicsPath()){
    path.AddLines(points);
    using(var edge=new Pen(Color.FromArgb(105,96,65),105)){edge.LineJoin=LineJoin.Round;edge.StartCap=edge.EndCap=LineCap.Round;g.DrawPath(edge,path);}
    using(var road=new Pen(Color.FromArgb(155,139,102),83)){road.LineJoin=LineJoin.Round;road.StartCap=road.EndCap=LineCap.Round;g.DrawPath(road,path);}
   }
   using(var rut=new Pen(Color.FromArgb(104,123,108,77),2)){
    for(int i=1;i<points.Length;i++){
     PointF a=points[i-1],b=points[i];float dx=b.X-a.X,dy=b.Y-a.Y;float length=(float)Math.Sqrt(dx*dx+dy*dy);if(length<1)continue;
     float nx=-dy/length*17,ny=dx/length*17;
     g.DrawLine(rut,a.X+nx,a.Y+ny,b.X+nx,b.Y+ny);g.DrawLine(rut,a.X-nx,a.Y-ny,b.X-nx,b.Y-ny);
    }
   }
  }
  static void Courtyard(Graphics g,RectangleF bounds,RectangleF visible){
   using(var dirt=new SolidBrush(Color.FromArgb(133,126,96)))g.FillRectangle(dirt,bounds);
   using(var stone=new Pen(Color.FromArgb(41,68,65,50),1)){
    for(int y=(int)bounds.Top;y<bounds.Bottom;y+=32){if(y<visible.Top||y>visible.Bottom)continue;g.DrawLine(stone,bounds.Left,y,bounds.Right,y);
     int shift=(y/32%2)*28;for(int x=(int)bounds.Left+shift;x<bounds.Right;x+=56)g.DrawLine(stone,x,y,x,y+32);}
   }
  }
  static void Grass(Graphics g,RectangleF visible){
   using(var light=new Pen(Color.FromArgb(55,203,187,114),1))
   using(var dark=new Pen(Color.FromArgb(55,47,66,39),1)){
    for(int y=Math.Max(0,(int)visible.Top/29*29);y<Math.Min(SceneWorld.WorldHeight,visible.Bottom);y+=29)
     for(int x=Math.Max(0,(int)visible.Left/29*29);x<Math.Min(SceneWorld.WorldWidth,visible.Right);x+=29){
      if(x>125&&x<777&&y>727&&y<1407)continue;
      int h=Hash(x,y);float xx=x+h%23,yy=y+(h/29)%19;g.DrawLine((h&1)==0?light:dark,xx,yy,xx+2,yy-4);
     }
   }
  }
  static void Landmarks(Graphics g){
   Sign(g,853,1001,"驿道");Sign(g,1560,610,"烽台");
   using(var wood=new Pen(Color.FromArgb(80,61,42),5)){g.DrawLine(wood,450,777,450,712);g.DrawLine(wood,450,714,488,714);}
   using(var flag=new SolidBrush(Color.FromArgb(141,42,33)))g.FillPolygon(flag,new[]{new PointF(453,715),new PointF(496,719),new PointF(485,734),new PointF(453,730)});
   using(var cord=new Pen(Color.FromArgb(176,145,90),1))g.DrawLine(cord,450,712,450,777);
  }
  static void Sign(Graphics g,float x,float y,string text){
   using(var post=new SolidBrush(Color.FromArgb(77,58,37)))g.FillRectangle(post,x-3,y-9,6,39);
   using(var board=new SolidBrush(Color.FromArgb(153,118,65)))g.FillRectangle(board,x-23,y-16,46,19);
   Caption(g,text,x,y-15,SmallFont,Color.FromArgb(40,35,25));
  }
  static void Rock(Graphics g,RectangleF r){
   using(var shadow=new SolidBrush(Color.FromArgb(60,26,31,24)))g.FillEllipse(shadow,r.X+5,r.Y+r.Height/2,r.Width+6,r.Height/2+6);
   var points=new[]{new PointF(r.X,r.Y+r.Height*.7f),new PointF(r.X+r.Width*.14f,r.Y+r.Height*.2f),new PointF(r.X+r.Width*.58f,r.Y),new PointF(r.Right,r.Y+r.Height*.4f),new PointF(r.Right-r.Width*.14f,r.Bottom),new PointF(r.X+r.Width*.3f,r.Bottom)};
   using(var body=new SolidBrush(Color.FromArgb(106,108,91)))g.FillPolygon(body,points);
   using(var top=new Pen(Color.FromArgb(154,151,123),2))g.DrawLines(top,new[]{points[0],points[1],points[2],points[3]});
   using(var crack=new Pen(Color.FromArgb(71,76,65),1))g.DrawLine(crack,r.X+r.Width*.6f,r.Y+4,r.X+r.Width*.43f,r.Bottom-2);
  }
  static void Tree(Graphics g,PointF p){
   using(var shadow=new SolidBrush(Color.FromArgb(49,24,34,24)))g.FillEllipse(shadow,p.X-18,p.Y-2,46,17);
   using(var trunk=new SolidBrush(Color.FromArgb(67,53,35)))g.FillRectangle(trunk,p.X-3,p.Y-28,6,34);
   using(var outer=new SolidBrush(Color.FromArgb(39,62,45)))g.FillPolygon(outer,new[]{new PointF(p.X,p.Y-71),new PointF(p.X-27,p.Y-18),new PointF(p.X+26,p.Y-18)});
   using(var middle=new SolidBrush(Color.FromArgb(49,77,49)))g.FillPolygon(middle,new[]{new PointF(p.X-2,p.Y-74),new PointF(p.X-22,p.Y-33),new PointF(p.X+19,p.Y-33)});
   using(var tip=new SolidBrush(Color.FromArgb(70,91,56)))g.FillPolygon(tip,new[]{new PointF(p.X-2,p.Y-74),new PointF(p.X-15,p.Y-47),new PointF(p.X+10,p.Y-47)});
  }
  static void Structure(Graphics g,SceneStructure s){
   var b=s.Bounds;
   if(s.Kind=="wall"){Wall(g,b);return;}
   if(s.Kind=="well"){
    using(var shadow=new SolidBrush(Color.FromArgb(60,25,31,24)))g.FillEllipse(shadow,b.X+4,b.Y+6,b.Width,b.Height);
    using(var stone=new SolidBrush(Color.FromArgb(128,127,105)))g.FillEllipse(stone,b);
    using(var water=new SolidBrush(Color.FromArgb(31,54,53)))g.FillEllipse(water,b.X+6,b.Y+5,b.Width-12,b.Height-10);
    using(var rim=new Pen(Color.FromArgb(175,163,126),3))g.DrawEllipse(rim,b.X+2,b.Y+2,b.Width-4,b.Height-4);
    return;
   }
   if(s.Kind=="ruin"){Ruin(g,b);return;}
   if(s.Kind=="beacon"){Beacon(g,b);return;}
   Building(g,b,s.Kind=="command"?Color.FromArgb(112,50,37):s.Kind=="storehouse"?Color.FromArgb(85,83,66):Color.FromArgb(99,65,46));
   Caption(g,s.Name,b.X+b.Width/2,b.Bottom+6,SmallFont,Color.FromArgb(229,217,177));
  }
  static void Wall(Graphics g,RectangleF b){
   using(var shadow=new SolidBrush(Color.FromArgb(54,20,28,22)))g.FillRectangle(shadow,b.X+5,b.Y+7,b.Width,b.Height);
   using(var wall=new SolidBrush(Color.FromArgb(92,96,79)))g.FillRectangle(wall,b);
   using(var top=new Pen(Color.FromArgb(160,155,120),3)){g.DrawLine(top,b.Left,b.Top,b.Right,b.Top);g.DrawLine(top,b.Left,b.Top,b.Left,b.Bottom);}
   using(var seam=new Pen(Color.FromArgb(57,65,52),2)){
    if(b.Width>b.Height){for(float x=b.Left+18;x<b.Right;x+=20)g.DrawLine(seam,x,b.Top+4,x,b.Bottom-1);}
    else{for(float y=b.Top+18;y<b.Bottom;y+=20)g.DrawLine(seam,b.Left+4,y,b.Right-1,y);}
   }
   using(var border=new Pen(Color.FromArgb(52,59,46),2))g.DrawRectangle(border,b.X,b.Y,b.Width,b.Height);
  }
  static void Building(Graphics g,RectangleF b,Color roof){
   using(var shadow=new SolidBrush(Color.FromArgb(66,25,29,23)))g.FillRectangle(shadow,b.X+10,b.Y+13,b.Width+3,b.Height+4);
   using(var body=new SolidBrush(Color.FromArgb(174,155,114)))g.FillRectangle(body,b);
   using(var foundation=new SolidBrush(Color.FromArgb(74,76,62)))g.FillRectangle(foundation,b.Left,b.Bottom-12,b.Width,12);
   using(var timber=new Pen(Color.FromArgb(78,57,38),5)){
    g.DrawLine(timber,b.Left+7,b.Top+7,b.Left+7,b.Bottom-4);g.DrawLine(timber,b.Right-7,b.Top+7,b.Right-7,b.Bottom-4);
    g.DrawLine(timber,b.Left+5,b.Bottom-24,b.Right-5,b.Bottom-24);
   }
   var outline=new[]{new PointF(b.Left-10,b.Top+3),new PointF(b.X+b.Width/2,b.Top-19),new PointF(b.Right+10,b.Top+3),new PointF(b.Right+8,b.Bottom-34),new PointF(b.Left-8,b.Bottom-34)};
   using(var r=new SolidBrush(roof))g.FillPolygon(r,outline);
   using(var tile=new Pen(Color.FromArgb(79,205,178,121),1)){
    for(float y=b.Top+7;y<b.Bottom-35;y+=12)g.DrawLine(tile,b.Left-7,y,b.Right+7,y);
    for(float x=b.Left+4;x<b.Right;x+=14)g.DrawLine(tile,x,b.Top+1,x,b.Bottom-35);
   }
   using(var ridgePen=new Pen(Color.FromArgb(46,44,34),4))g.DrawLine(ridgePen,b.X+b.Width/2,b.Top-16,b.X+b.Width/2,b.Bottom-35);
   using(var border=new Pen(Color.FromArgb(46,44,34),3))g.DrawPolygon(border,outline);
   float doorX=b.X+b.Width/2;
   using(var door=new SolidBrush(Color.FromArgb(51,42,30)))g.FillRectangle(door,doorX-13,b.Bottom-31,26,29);
   using(var frame=new Pen(Color.FromArgb(150,111,59),2))g.DrawRectangle(frame,doorX-13,b.Bottom-31,26,29);
   Lamp(g,b.Right-19,b.Bottom-22);
  }
  static void Ruin(Graphics g,RectangleF b){
   using(var shadow=new SolidBrush(Color.FromArgb(50,21,27,21)))g.FillRectangle(shadow,b.X+7,b.Y+9,b.Width,b.Height);
   using(var body=new SolidBrush(Color.FromArgb(102,103,85)))g.FillPolygon(body,new[]{new PointF(b.Left,b.Bottom),new PointF(b.Left,b.Top+12),new PointF(b.Left+b.Width*.3f,b.Top),new PointF(b.Left+b.Width*.46f,b.Top+17),new PointF(b.Right,b.Top+8),new PointF(b.Right,b.Bottom)});
   using(var seam=new Pen(Color.FromArgb(64,72,58),2)){for(float y=b.Top+22;y<b.Bottom;y+=19)g.DrawLine(seam,b.Left+2,y,b.Right-2,y);}
   using(var top=new Pen(Color.FromArgb(152,147,114),2))g.DrawLine(top,b.Left+2,b.Top+14,b.Left+b.Width*.3f,b.Top+2);
  }
  static void Beacon(Graphics g,RectangleF b){
   using(var shadow=new SolidBrush(Color.FromArgb(70,27,32,24)))g.FillEllipse(shadow,b.X+15,b.Y+b.Height*.55f,b.Width+14,b.Height*.6f);
   using(var baseBrush=new SolidBrush(Color.FromArgb(87,89,75)))g.FillRectangle(baseBrush,b);
   using(var inner=new SolidBrush(Color.FromArgb(118,115,89)))g.FillRectangle(inner,b.X+10,b.Y+10,b.Width-20,b.Height-20);
   using(var dark=new SolidBrush(Color.FromArgb(46,52,42)))g.FillRectangle(dark,b.X+25,b.Y+27,b.Width-50,b.Height-51);
   using(var crenel=new SolidBrush(Color.FromArgb(145,137,104))){
    for(float x=b.Left;x<b.Right-8;x+=26){g.FillRectangle(crenel,x,b.Top-4,15,15);g.FillRectangle(crenel,x,b.Bottom-11,15,15);}
    for(float y=b.Top+20;y<b.Bottom-18;y+=26){g.FillRectangle(crenel,b.Left-3,y,15,15);g.FillRectangle(crenel,b.Right-12,y,15,15);}
   }
   using(var ashes=new SolidBrush(Color.FromArgb(72,62,47)))g.FillEllipse(ashes,b.X+b.Width/2-19,b.Y+b.Height/2-12,38,24);
   Caption(g,"废烽燧",b.X+b.Width/2,b.Bottom+8,SmallFont,Color.FromArgb(224,211,172));
  }
  static void Lamp(Graphics g,float x,float y){
   using(var glow=new SolidBrush(Color.FromArgb(24,255,205,112)))g.FillEllipse(glow,x-16,y-16,32,32);
   using(var lamp=new SolidBrush(Color.FromArgb(232,183,91)))g.FillEllipse(lamp,x-3,y-4,6,9);
  }
  static void Npc(Graphics g,SceneNpc npc,SceneWorld world){
   float dx=world.PlayerX-npc.X,dy=world.PlayerY-npc.Y;
   bool near=dx*dx+dy*dy<80*80;
   using(var ring=new Pen(near?Color.FromArgb(236,203,117):Color.FromArgb(114,182,168,114),1.5f))g.DrawEllipse(ring,npc.X-15,npc.Y-10,30,20);
   Figure(g,npc.X,npc.Y,0,1,npc.Id=="quartermaster"?Color.FromArgb(75,88,95):Color.FromArgb(121,100,62),false);
   Caption(g,npc.Name,npc.X,npc.Y-42,LabelFont,Color.FromArgb(240,222,169));
   if(near)Caption(g,"E · 交谈",npc.X,npc.Y+18,SmallFont,Color.FromArgb(254,235,170));
  }
  static void Agent(Graphics g,SceneAgentSnapshot agent){
   Figure(g,agent.X,agent.Y,agent.FacingX,agent.FacingY,Color.FromArgb(109,83,65),false);
   string activity=agent.Blocked?"道路受阻":agent.Mode==SceneAgentMode.Chase?"靠近来客":agent.Mode==SceneAgentMode.Return?"返回岗点":"巡路";
   Caption(g,"巡路哨 · "+activity,agent.X,agent.Y-43,SmallFont,Color.FromArgb(235,211,159));
  }
  static void Player(Graphics g,SceneWorld world){
   using(var shadow=new SolidBrush(Color.FromArgb(38,245,209,103)))g.FillEllipse(shadow,world.PlayerX-23,world.PlayerY-18,46,36);
   using(var ring=new Pen(Color.FromArgb(230,224,190,112),2))g.DrawEllipse(ring,world.PlayerX-13,world.PlayerY-9,26,18);
   Figure(g,world.PlayerX,world.PlayerY,world.FacingX,world.FacingY,Color.FromArgb(141,53,39),true);
   using(var direction=new Pen(Color.FromArgb(233,221,168),2))g.DrawLine(direction,world.PlayerX+world.FacingX*15,world.PlayerY+world.FacingY*15,world.PlayerX+world.FacingX*21,world.PlayerY+world.FacingY*21);
  }
  static void Figure(Graphics g,float x,float y,float fx,float fy,Color coat,bool player){
   using(var shadow=new SolidBrush(Color.FromArgb(90,19,24,20)))g.FillEllipse(shadow,x-10,y-3,23,10);
   using(var boots=new Pen(Color.FromArgb(43,38,29),4)){g.DrawLine(boots,x-4,y-5,x-5,y+2);g.DrawLine(boots,x+4,y-5,x+5,y+2);}
   using(var body=new SolidBrush(coat))g.FillPolygon(body,new[]{new PointF(x-7,y-21),new PointF(x+7,y-21),new PointF(x+9,y-4),new PointF(x-9,y-4)});
   using(var sash=new Pen(Color.FromArgb(189,150,83),2))g.DrawLine(sash,x-7,y-10,x+7,y-10);
   using(var skin=new SolidBrush(Color.FromArgb(188,153,108)))g.FillEllipse(skin,x-5,y-31,10,11);
   using(var hat=new SolidBrush(player?Color.FromArgb(60,68,63):Color.FromArgb(42,47,40)))g.FillPie(hat,x-7,y-34,14,13,180,180);
   using(var arm=new Pen(Color.FromArgb(56,53,40),3)){g.DrawLine(arm,x-8,y-19,x-11,y-10);g.DrawLine(arm,x+8,y-19,x+11,y-10);}
   if(player)using(var gear=new Pen(Color.FromArgb(89,88,70),2))g.DrawLine(gear,x+11,y-20,x+13,y+1);
  }
  static void Caption(Graphics g,string text,float x,float y,Font font,Color color){
   SizeF size=g.MeasureString(text,font);float left=x-size.Width/2;
   using(var shadow=new SolidBrush(Color.FromArgb(210,25,31,25)))g.DrawString(text,font,shadow,left+1,y+1);
   using(var brush=new SolidBrush(color))g.DrawString(text,font,brush,left,y);
  }
 }
}
