using System;
using System.Collections.Generic;
using System.Drawing;

namespace GrimDynasty.Scene {
 public sealed class SceneNpc {
  public readonly string Id,Name,Role,Hint;
  public readonly float X,Y;
  public SceneNpc(string id,string name,float x,float y,string role){Id=id;Name=name;X=x;Y=y;Role=role;Hint=role;}
 }
 public sealed class SceneStructure {
  public readonly string Kind,Name;
  public readonly RectangleF Bounds;
  public SceneStructure(string kind,string name,float x,float y,float width,float height){Kind=kind;Name=name;Bounds=new RectangleF(x,y,width,height);}
 }

 // Project-authored presentation world for the Liaoxi slice. Pixel-space movement,
 // geometry, and collision are an independent view/input adapter, not a recovered
 // native physics, pathfinding, combat, equipment, or trade implementation.
 public sealed class SceneWorld {
  public const int WorldWidth=2200,WorldHeight=1600;
  public const float PlayerRadius=10f,MoveSpeed=185f,MaxDeltaSeconds=0.1f;
  public float PlayerX {get;private set;}
  public float PlayerY {get;private set;}
  public float FacingX {get;private set;}
  public float FacingY {get;private set;}

  static readonly SceneNpc[] npcData={
   new SceneNpc("quartermaster","营务官",520,1040,"堡内领取与交付军粮任务"),
   new SceneNpc("supply-clerk","补给吏",1660,560,"驿道尽头核验并领回物资")
  };
  static readonly SceneStructure[] structureData={
   new SceneStructure("wall","北垣",100,700,720,20),
   new SceneStructure("wall","南垣",100,1420,720,20),
   new SceneStructure("wall","西垣",100,720,20,700),
   // Right-wall opening y=1000..1110 is the only fort gateway.
   new SceneStructure("wall","东垣北段",800,720,20,280),
   new SceneStructure("wall","东垣南段",800,1110,20,310),
   new SceneStructure("barracks","边军营房",160,800,240,130),
   new SceneStructure("storehouse","军粮仓",560,790,175,150),
   new SceneStructure("command","驿堡公所",170,1110,230,175),
   new SceneStructure("well","堡井",565,1210,36,36),
   new SceneStructure("beacon","废烽燧",1770,250,165,170),
   new SceneStructure("ruin","烽燧北垣",1600,150,440,18),
   new SceneStructure("ruin","烽燧东垣",2020,168,18,370),
   new SceneStructure("ruin","断垣",1600,168,18,202),
   new SceneStructure("ruin","残墙",1800,600,240,18)
  };
  static readonly PointF[] roadData={
   new PointF(430,1150),new PointF(520,1050),new PointF(760,1050),
   new PointF(1000,1050),new PointF(1240,850),new PointF(1450,670),new PointF(1660,560),
   new PointF(1700,475)
  };
  static readonly PointF[] treeData={
   new PointF(900,800),new PointF(970,700),new PointF(1050,790),
   new PointF(1220,580),new PointF(1360,450),new PointF(1470,340),
   new PointF(1500,1320),new PointF(1680,1270),new PointF(1920,1190),
   new PointF(2040,1020),new PointF(850,1380),new PointF(940,1450),
   new PointF(1160,1320),new PointF(1460,970),new PointF(2040,780),
   new PointF(340,570),new PointF(260,490),new PointF(570,540),
   new PointF(700,300),new PointF(830,380),new PointF(1170,180),
   new PointF(1380,180),new PointF(2100,1420),new PointF(2020,1340)
  };
  static readonly RectangleF[] rockData={
   new RectangleF(1020,1280,88,60),new RectangleF(1160,320,80,52),
   new RectangleF(1740,1050,92,62),new RectangleF(2070,800,65,90),
   new RectangleF(1750,690,55,42),new RectangleF(1370,1260,100,82),
   new RectangleF(480,480,88,64),new RectangleF(970,250,60,40)
  };
  static readonly PointF[] waypointData={
   new PointF(430,1150),new PointF(520,1040),new PointF(760,1050),
   new PointF(850,1050),new PointF(1000,1050),new PointF(1240,850),
   new PointF(1450,670),new PointF(1660,560)
  };
  static readonly RectangleF[] collisionData=BuildCollisions();
  // Copies keep callers from changing world geometry or another session's map.
  public SceneNpc[] Npcs {get{return (SceneNpc[])npcData.Clone();}}
  public SceneStructure[] Structures {get{return (SceneStructure[])structureData.Clone();}}
  public RectangleF[] CollisionRects {get{return (RectangleF[])collisionData.Clone();}}
  public PointF[] RoadPoints {get{return (PointF[])roadData.Clone();}}
  public PointF[] Trees {get{return (PointF[])treeData.Clone();}}
  public RectangleF[] Rocks {get{return (RectangleF[])rockData.Clone();}}
  public PointF[] TestWaypoints {get{return (PointF[])waypointData.Clone();}}

  public SceneWorld(){PlayerX=430;PlayerY=1150;FacingX=0;FacingY=1;}
  internal SceneWorld CopyForCommit(){return new SceneWorld{PlayerX=PlayerX,PlayerY=PlayerY,FacingX=FacingX,FacingY=FacingY};}
  static RectangleF[] BuildCollisions(){
   var rects=new List<RectangleF>();
   foreach(var structure in structureData)rects.Add(structure.Bounds);
   foreach(var rock in rockData)rects.Add(rock);
   // Only each tree's trunk collides; its painted crown may overhang the ground.
   foreach(var tree in treeData)rects.Add(new RectangleF(tree.X-9,tree.Y-9,18,18));
   return rects.ToArray();
  }
  static bool Finite(float value){return !Single.IsNaN(value)&&!Single.IsInfinity(value);}
  static float Clamp(float value,float lower,float upper){return Math.Max(lower,Math.Min(upper,value));}
  public bool IsWalkable(float x,float y){
   if(!Finite(x)||!Finite(y)||x<PlayerRadius||y<PlayerRadius||x>WorldWidth-PlayerRadius||y>WorldHeight-PlayerRadius)return false;
   foreach(RectangleF rect in collisionData){
    float dx=x-Clamp(x,rect.Left,rect.Right),dy=y-Clamp(y,rect.Top,rect.Bottom);
    if(dx*dx+dy*dy<PlayerRadius*PlayerRadius)return false;
   }
   return true;
  }
  public bool TrySetPosition(float x,float y){
   if(!IsWalkable(x,y))return false;PlayerX=x;PlayerY=y;return true;
  }
  public bool Move(float dx,float dy,float dt){
   if(!Finite(dx)||!Finite(dy)||!Finite(dt)||dt<0)throw new ArgumentOutOfRangeException("movement","Finite direction and nonnegative delta time required");
   dt=Math.Min(dt,MaxDeltaSeconds);if(dt==0||dx==0&&dy==0)return false;
   // Scale before length calculation to avoid overflow for finite but huge input.
   float largest=Math.Max(Math.Abs(dx),Math.Abs(dy));
   if(largest>1){dx/=largest;dy/=largest;}
   double length=Math.Sqrt((double)dx*dx+(double)dy*dy);
   if(length==0)return false;
   FacingX=(float)(dx/length);FacingY=(float)(dy/length);
   if(length>1){dx=FacingX;dy=FacingY;}
   float mx=dx*MoveSpeed*dt,my=dy*MoveSpeed*dt;
   int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(Math.Abs(mx),Math.Abs(my))/4f));
   float sx=mx/steps,sy=my/steps;float oldX=PlayerX,oldY=PlayerY;
   for(int i=0;i<steps;i++){
    if(IsWalkable(PlayerX+sx,PlayerY))PlayerX+=sx;
    if(IsWalkable(PlayerX,PlayerY+sy))PlayerY+=sy;
   }
   return PlayerX!=oldX||PlayerY!=oldY;
  }
 }
}
