const double SearchRange_Gun = 15 * (Math.PI / 180);          
const double SearchRange     = 20000;
    
const double ApproachRange_short  = 50;
const double ApproachRange_middle = 400;
const double ApproachRange_long   = 1000;

const double AttackRange     = 800;    
        
List<IMyTerminalBlock>        list        = new List<IMyTerminalBlock>();        
List<IMySmallMissileLauncher> listWeapons = new List<IMySmallMissileLauncher>();       
List<IMyCameraBlock>          listCameras = new List<IMyCameraBlock>();
    
IMyRemoteControl remote;
IMyCameraBlock mainCamera;
          
Vector3D v3dSpawendPosition = new Vector3D(0, 0, 0);        
            
public Program() {
     
    // The constructor, called only once every session and     
    // always before any other method is called. Use it to     
    // initialize your script.      
    //          
    // The constructor is optional and can be removed if not     
    // needed.
     
}
     

     
public void Save() {
     
    // Called when the program needs to save its state. Use     
    // this method to save your state to the Storage field     
    // or some other means.      
    //      
    // This method is optional and can be removed if not     
    // needed.
     
}
     

     
public void Main(string argument)    
{
     
    // The main entry point of the script, invoked every time     
    // one of the programmable block's Run actions are invoked.     
    //      
    // The method itself is required, but the argument above     
    // can be removed if not needed.
           
   Vector3D player = new Vector3D(0, 0, 0);        
        
   try     
   {     
      if ( ! initialize())        
      {        
         Echo("Initialize failed.");      
         return;        
      }        
        
      if ( ! WeaponDiagonosis())        
      {      
         Echo("Weapon error.");      
         doEscape(100f);        
         return;        
      }
      
      if ( ! getMainCamera() )        
      {      
         Echo("Sensor error.");      
         doEscape(100f);        
         return;        
      }  
           
      doNavigation(out player);    
          
      doFire(player);    
   }     
   catch (Exception e)    
   {     
      // Get the line number from the stack frame     
      Echo("    message: " + e.Message);     
      Echo("stack trace: : " + e.StackTrace);     
   }     
}     
        
bool initialize()        
{          
   if (this.Storage == null || this.Storage == "")        
   {        
      v3dSpawendPosition = Me.GetPosition();        
      this.Storage = v3dSpawendPosition.ToString();        
   }        
   else        
   {        
      Vector3D.TryParse(this.Storage, out v3dSpawendPosition);        
   }        
        
   if ( ! getRemoteControl())        
   {        
      return false;        
   }    
           
   getWeapons();       
   return true;       
}        
        
bool getRemoteControl()       
{            
    GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list);        
    if (list.Count <= 0)       
    {            
        return false;       
    }            
    remote = (IMyRemoteControl)list[0];       
    return true;            
}       
       
void getWeapons()       
{             
    GridTerminalSystem.GetBlocksOfType<IMySmallMissileLauncher>(list);       
               
    for (int i = 0; i < list.Count; ++i)       
    {       
       listWeapons.Add((IMySmallMissileLauncher)list[i]);        
    }       
}

bool getMainCamera()       
{             
    GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(listCameras);        
    if (listCameras.Count <= 0)       
    {            
        return false;       
    }

    for (int i = 0; i < listCameras.Count; ++i)
    {
        if ( ! listCameras[i].IsFunctional ) { continue; }
        mainCamera = (IMyCameraBlock)listCameras[i];
        return true;      
    }
    return false;    
}  
    
bool WeaponDiagonosis()           
{           
    if (listWeapons.Count <= 0)            
    {           
        return false;          
    }           
          
    for (int i = 0; i < listWeapons.Count; ++i)             
    {             
        var weapon = listWeapons[i];             
        if ( ! weapon.IsFunctional) { continue; }            
        if (weapon.HasInventory && !weapon.GetInventory(0).IsItemAt(0)) { continue; }           
        return true;      
    }            
    return false;        
}
          
bool SearchPlayer(out Vector3D player)           
{          
    bool success = remote.GetNearestPlayer(out player);          
          
    if ( ! success)          
    {    
       return false;          
    }          
          
    if (getDistance(player, Me.GetPosition()) > SearchRange)          
    {    
       doPatrol(30f);    
       return false;          
    }           
          
    return true;           
}          
          
bool isShootPlayer(Vector3D player)          
{            
   if (getDistance(player, Me.GetPosition()) > AttackRange)               
   {            
       return false;               
   }          
        
   // check player on line of fire
   var reference = mainCamera;          
   Vector3D posSensor = reference.GetPosition();          
             
   var forwardPos = reference.Position + Base6Directions.GetIntVector(reference.Orientation.TransformDirection(Base6Directions.Direction.Forward));   
   var forward = reference.CubeGrid.GridIntegerToWorld(forwardPos);   
   var forwardVector = Vector3D.Normalize(forward - reference.GetPosition());        
             
   Vector3D target_raw = reference.GetPosition() + AttackRange * forwardVector;   
      
   // change to fire line.   
   Vector3D target = new Vector3D(target_raw.X, target_raw.Y + 2.5, target_raw.Z);   
        
   float r = (float)Math.Tan(SearchRange_Gun) * (float)AttackRange;          
        
   Double dotProduct = getDotProduct(Vector3D.Subtract(player, posSensor), Vector3D.Subtract(target, posSensor));          
        
   Vector3D crossProduct = getCrossProduct(Vector3D.Subtract(player, posSensor), Vector3D.Subtract(target, posSensor));          
     
   bool flag1 = 0 <= dotProduct;  
   bool flag2 = dotProduct <= AttackRange * AttackRange;  
   bool flag3 = getDistance(crossProduct, new Vector3D(0,0,0)) <= getDistance(player, posSensor)*r*AttackRange / Math.Sqrt(r*r + AttackRange * AttackRange);  
     
   if( true        
      && flag1  
      && flag2  
      && flag3  
   )  
   {  
      Echo("target found.");  
      return true;        
   }  
   else  
   {  
      Echo("target not found(" + flag1 + flag2 + flag3 + ").");  
      return false;        
   }        
}        
    
void doNavigation(out Vector3D player)    
{
   if ( ! SearchPlayer(out player))    
   {    
      return;    
   }    
    
   if (getDistance(player, Me.GetPosition()) <= ApproachRange_short)          
   {    
      doApproach(player, 1f);
   }
   if (getDistance(player, Me.GetPosition()) <= ApproachRange_middle)          
   {    
      doApproach(player, 30f);
   }
   if (getDistance(player, Me.GetPosition()) <= ApproachRange_middle)          
   {    
      doApproach(player, 75f);
   }

   doApproach(player, 100f);
}    
    
void doEscape(float speed)        
{
   Echo("Starting Escape");     
   remote.SetValue( "SpeedLimit", speed );
   remote.ClearWaypoints();        
   remote.AddWaypoint(v3dSpawendPosition, "SpawendPosition");        
   remote.SetAutoPilotEnabled(true);        
}        
           
void doApproach(Vector3D player, float speed)        
{        
   Echo("Starting Approach");     
   remote.SetValue( "SpeedLimit", speed );
   remote.ClearWaypoints();        
   remote.AddWaypoint(player, "Player");        
   remote.SetAutoPilotEnabled(true);        
}        
        
void doPatrol(float speed)        
{     
   Echo("Starting Patrol");
   remote.SetValue( "SpeedLimit", speed );
   remote.ClearWaypoints();        
   remote.AddWaypoint(v3dSpawendPosition, "SpawendPosition");        
   remote.SetAutoPilotEnabled(true);        
}        
        
void doFire(Vector3D player)        
{        
   if ( ! isShootPlayer(player))        
   {        
      for (int i = 0; i < listWeapons.Count; ++i)        
      {        
         listWeapons[i].ApplyAction("Shoot_Off");  
         interiorLightOnOff("OnOff_Off");  
      }        
      return;        
   }        
   for (int i = 0; i < listWeapons.Count; ++i)        
   {        
      interiorLightOnOff("OnOff_On");  
      playWarn();  
      listWeapons[i].ApplyAction("Shoot_On");  
   }        
}  
  
void interiorLightOnOff(string strAction)        
{  
    ITerminalAction action;  
  
    GridTerminalSystem.SearchBlocksOfName("Interior Light Lock", list);  
      
    for (int i = 0; i < list.Count; ++i)       
    {  
       action = list[i].GetActionWithName(strAction);  
       //Echo(list[i].CustomName);  
       if ( action != null ) { action.Apply( list[i] ); }  
    }  
}  
  
void playWarn()        
{  
    ITerminalAction action;  
  
    GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(list);        
    if (list.Count <= 0)       
    {            
        return;       
    }     
      
    for (int i = 0; i < list.Count; ++i)       
    {  
       action = list[i].GetActionWithName("PlaySound");         
       if ( action != null ) { action.Apply( list[i] ); }  
    }  
}  
        
double getDistance(Vector3D start, Vector3D end)        
{        
   double nX = start.X - end.X;        
   double nY = start.Y - end.Y;        
   double nZ = start.Z - end.Z;        
        
   return Math.Sqrt(nX * nX + nY * nY + nZ * nZ);        
}        
        
double getDotProduct(Vector3D start, Vector3D end)        
{        
   return start.X * end.X + start.Y * end.Y + start.Z * end.Z;        
}        
        
Vector3D getCrossProduct(Vector3D start, Vector3D end)        
{        
   return new Vector3D(        
      start.Y * end.Z - start.Z * end.Y,        
      start.Z * end.X - start.X * end.Z,        
      start.X * end.Y - start.Y * end.X        
   );        
} 