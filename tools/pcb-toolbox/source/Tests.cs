using System;
internal static class Tests {
 [STAThread] static int Main() {
  double reference = TraceMath.Area(10,20);
  if (Math.Abs(reference - 513.1) > 0.1) throw new Exception("Published example mismatch");
  int count=1;
  foreach(double i in new double[]{0.01,0.1,1,3,10,30})
   foreach(double dt in new double[]{2,10,20,50,100})
    foreach(double t in new double[]{17.5,35,70,105}) {
     double w=TraceMath.Width(i,dt,t);
     double round=TraceMath.Current(w,dt,t);
     if(Math.Abs(round-i)>1e-9) throw new Exception("Round trip");
     if(TraceMath.Width(i,dt,t*2)>=w) throw new Exception("Thickness monotonicity");
     count+=2;
    }
  if(TraceMath.Width(3,10,35)<=TraceMath.Width(1,10,35)) throw new Exception("Current monotonicity");
  if(TraceMath.Width(3,20,35)>=TraceMath.Width(3,10,35)) throw new Exception("Rise monotonicity");
  foreach(double bad in new double[]{0,-1,31}) {
   bool rejected=false; try{TraceMath.Area(bad,10);}catch(ArgumentOutOfRangeException){rejected=true;}
   if(!rejected)throw new Exception("Invalid input accepted"); count++;
  }
  using(MainForm form=new MainForm()) {form.CreateControl();}
  Console.WriteLine("PASS: "+(count+3)+" checks; published 10A/20C example area = "+reference.ToString("F4")+" MIL^2");
  Console.WriteLine("3A/10C/35um: "+(TraceMath.Width(3,10,35)*0.0254).ToString("F6")+" mm");
  return 0;
 }
}
