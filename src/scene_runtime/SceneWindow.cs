using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GrimDynasty.Scene {
 public sealed class SceneWindow:Form {
  readonly SceneStore store;readonly string slot;SceneState state;
  readonly HashSet<Keys> held=new HashSet<Keys>();readonly Timer timer=new Timer();readonly Stopwatch clock=new Stopwatch();
  readonly bool smoke;readonly string smokeDirectory;int frames;double last;
  public bool SmokeSaveSucceeded {get;private set;}
  string message="WASD / 方向键移动，靠近营务官按 E。";
  public SceneWindow(SceneStore store,string slot,SceneState state,bool smoke,string smokeDirectory){
   this.store=store;this.slot=slot;this.state=state;this.smoke=smoke;this.smokeDirectory=smokeDirectory;
   if(state.MigratedVisualPatrol)message="已读取旧场景；本版新增巡路哨，其位置将与角色一起保存。";
   Text="恐怖大明 · 辽西驿堡 — 场景检查点";ClientSize=new Size(1100,760);MinimumSize=new Size(800,600);StartPosition=FormStartPosition.CenterScreen;KeyPreview=true;
   SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);
   timer.Interval=33;timer.Tick+=Tick;Shown+=delegate{clock.Start();timer.Start();};Deactivate+=delegate{held.Clear();};
  }
  void Tick(object sender,EventArgs e){
   double now=clock.Elapsed.TotalSeconds;float dt=(float)Math.Min(.05,now-last);last=now;
   float x=(held.Contains(Keys.D)||held.Contains(Keys.Right)?1:0)-(held.Contains(Keys.A)||held.Contains(Keys.Left)?1:0);
   float y=(held.Contains(Keys.S)||held.Contains(Keys.Down)?1:0)-(held.Contains(Keys.W)||held.Contains(Keys.Up)?1:0);
   state.World.Move(x,y,dt);state.TickAgents(dt);frames++;
   // Route an arrow through WinForms preprocessing and this window's message
   // handler, then let the normal timer move. No global keyboard injection.
   if(smoke&&frames==3)SmokeKey(Keys.Right,true);
   if(smoke&&frames==24)SmokeKey(Keys.Right,false);
   if(smoke&&frames==28){
    using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(Point.Empty,Size));bitmap.Save(Path.Combine(smokeDirectory,"window.png"),ImageFormat.Png);}
    var view=state.Session.View();
    File.WriteAllText(Path.Combine(smokeDirectory,"window-smoke.json"),GrimDynasty.Session.StrictJson.Canonical(new Dictionary<string,object>{
     {"frames",frames},{"x_bits",BitConverter.ToUInt32(BitConverter.GetBytes(state.World.PlayerX),0)},
     {"y_bits",BitConverter.ToUInt32(BitConverter.GetBytes(state.World.PlayerY),0)},
     {"form_width",Width},{"form_height",Height},{"client_width",ClientSize.Width},{"client_height",ClientSize.Height},
     {"money",view.money},{"character_id",view.character_id},{"input","Right arrow: WinForms preprocessing + window handler + timer"}}));Close();
   }
   Invalidate();
  }
  void SmokeKey(Keys key,bool down){
   var message=Message.Create(Handle,down?0x100:0x101,new IntPtr((int)key),IntPtr.Zero);
   if(!PreProcessMessage(ref message))WndProc(ref message);
  }
  protected override bool IsInputKey(Keys keyData){
   Keys key=keyData&Keys.KeyCode;
   return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||base.IsInputKey(keyData);
  }
  protected override void OnKeyDown(KeyEventArgs e){
   base.OnKeyDown(e);bool fresh=held.Add(e.KeyCode);
   if(!fresh)return;
   if(e.KeyCode==Keys.E){
    var result=state.Interact();
    if(result.Status!="Known")message="此操作暂不可用："+result.Reason;
    else if(result.Reason=="NoNearbyNpc")message="靠近营务官或补给吏，再按 E 交谈。";
    else if(result.Changed){message="任务进展已更新。";Save();}
    else message="此任务已经完成，奖励不会重复发放。";
   }
   if(e.KeyCode==Keys.F5)Save();
   if(e.KeyCode==Keys.F9)try{state=store.Load(slot);message="已载入最近保存的角色、任务和位置。";}catch(Exception ex){message="载入失败："+ex.Message;}
   if(e.KeyCode==Keys.Space)message="战斗尚未开放：当前版本没有已验证的伤害结算。";
   if(e.KeyCode==Keys.B||e.KeyCode==Keys.I)message="买卖与装备动作尚未开放，已有背包和任务状态可保存。";
   if(e.KeyCode==Keys.Escape)Close();
   e.Handled=true;
  }
  protected override void OnKeyUp(KeyEventArgs e){held.Remove(e.KeyCode);base.OnKeyUp(e);}
  bool Save(){try{store.Save(slot,state);message="已保存角色、任务和地图位置。";return true;}catch(Exception ex){message="保存失败："+ex.Message;return false;}}
  protected override void OnFormClosing(FormClosingEventArgs e){
   timer.Stop();held.Clear();bool saved=Save();if(smoke)SmokeSaveSucceeded=saved;
   if(!saved&&!smoke){if(MessageBox.Show(this,message+"\n仍要退出并放弃未保存变化吗？","保存失败",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes){e.Cancel=true;timer.Start();}}
   base.OnFormClosing(e);
  }
  protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
  protected override void OnPaint(PaintEventArgs e){SceneRenderer.Draw(e.Graphics,state,ClientSize.Width,ClientSize.Height);DrawHud(e.Graphics);}
  void DrawHud(Graphics g){
   var v=state.Session.View();using(var shade=new SolidBrush(Color.FromArgb(225,18,24,26)))using(var text=new SolidBrush(Color.FromArgb(238,226,197)))using(var gold=new SolidBrush(Color.FromArgb(222,180,95)))
   using(var title=new Font("Microsoft YaHei",17,FontStyle.Bold))using(var body=new Font("Microsoft YaHei",10)){
    g.FillRectangle(shade,0,0,ClientSize.Width,92);g.DrawString("辽西驿堡  /  边军补给",title,gold,22,12);
    g.DrawString(v.name+"   ·   钱币 "+v.money+"   ·   背包 "+v.inventory_ids.Length+"   ·   存档 "+slot,body,text,23,48);
    string task=!v.delivery_accepted?"向堡内营务官报到":!v.delivery_completed?"交付随身的三份军粮（营务官 E）":!v.return_accepted?"沿驿路前往废烽燧，寻找补给吏":!v.return_completed?"向补给吏核验余粮（E）":"补给任务完成 · 两次奖励均已记录";
    g.DrawString(task,body,gold,23,70);
    g.FillRectangle(shade,0,ClientSize.Height-70,ClientSize.Width,70);g.DrawString(message,body,text,22,ClientSize.Height-62);
    g.DrawString("WASD 移动   E 交谈   F5 保存   F9 载入   Esc 保存退出   |   战斗/交易/装备待验证",body,gold,22,ClientSize.Height-33);
   }
  }
 }
}
