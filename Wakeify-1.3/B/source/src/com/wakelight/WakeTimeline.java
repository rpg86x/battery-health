package com.wakelight;
final class WakeTimeline {
 static final long LIGHT_MS=600000L;
 static int validLag(int value){return value>=0 && value<=30 && value%5==0?value:5;}
 static long musicDelay(int minutes){return validLag(minutes)*60000L;}
 static long end(boolean music,int minutes){return music?Math.max(LIGHT_MS,musicDelay(minutes)+12000L):LIGHT_MS;}
}
