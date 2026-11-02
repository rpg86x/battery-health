package com.wakelight;
import android.content.Context;
import android.hardware.camera2.*;
final class Torch {
 final CameraManager manager;final String id;
 Torch(Context context)throws Exception{
  manager=context.getSystemService(CameraManager.class);String found=null;
  for(String candidate:manager.getCameraIdList()){
   CameraCharacteristics c=manager.getCameraCharacteristics(candidate);
   if(Boolean.TRUE.equals(c.get(CameraCharacteristics.FLASH_INFO_AVAILABLE)) && Integer.valueOf(CameraCharacteristics.LENS_FACING_BACK).equals(c.get(CameraCharacteristics.LENS_FACING))){found=candidate;break;}
  }
  if(found==null)throw new Exception("Geen zaklamp beschikbaar.");id=found;
 }
 void on()throws Exception{manager.setTorchMode(id,true);}
 void off(){try{manager.setTorchMode(id,false);}catch(Exception ignored){}}
}
