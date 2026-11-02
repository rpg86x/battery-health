package com.wakelight;
public class WakeTimelineTest {
 public static void main(String[] args){
  for(int lag:new int[]{0,5,10,15,20,25,30}){
   if(WakeTimeline.musicDelay(lag)!=lag*60000L)throw new AssertionError("Delay mismatch");
   if(WakeTimeline.end(true,lag)<WakeTimeline.musicDelay(lag)+10000L)throw new AssertionError("Service ends before music confirmation");
   if(WakeTimeline.end(true,lag)<WakeTimeline.LIGHT_MS)throw new AssertionError("Light ends too soon");
  }
  if(WakeTimeline.end(false,30)!=600000L)throw new AssertionError("Music disabled should not extend light");
  if(WakeTimeline.musicDelay(99)!=300000L)throw new AssertionError("Invalid preference must use default five minutes");
  System.out.println("9 timeline cases passed: all seven lags, music disabled, invalid preference. Delayed music survives the ten-minute light cutoff.");
 }
}
