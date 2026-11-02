package com.wakelight;
import java.util.Calendar;
import java.util.TimeZone;
final class WeeklySchedule {
 static final int DEFAULT_DAYS = 30; // Monday through Thursday, Calendar day bits.
 static long next(long now, int days, int hour, int minute, TimeZone zone) {
  return next(now,days,hour,minute,hour,minute,zone);
 }
 static long next(long now, int days, int hour, int minute, int fridayHour, int fridayMinute, TimeZone zone) {
  return next(now,days,hour,minute,fridayHour,fridayMinute,hour,minute,zone);
 }
 static long next(long now, int days, int hour, int minute, int fridayHour, int fridayMinute, int sundayHour, int sundayMinute, TimeZone zone) {
  if(days==0 || hour<0 || hour>23 || minute<0 || minute>59) return 0;
  Calendar base=Calendar.getInstance(zone);base.setTimeInMillis(now);
  for(int offset=0;offset<=7;offset++) {
   Calendar candidate=(Calendar)base.clone();candidate.add(Calendar.DATE,offset);
   boolean friday=candidate.get(Calendar.DAY_OF_WEEK)==Calendar.FRIDAY;
   boolean sunday=candidate.get(Calendar.DAY_OF_WEEK)==Calendar.SUNDAY;
   candidate.set(Calendar.HOUR_OF_DAY,friday?fridayHour:sunday?sundayHour:hour);candidate.set(Calendar.MINUTE,friday?fridayMinute:sunday?sundayMinute:minute);candidate.set(Calendar.SECOND,0);candidate.set(Calendar.MILLISECOND,0);
   if((days & (1 << (candidate.get(Calendar.DAY_OF_WEEK)-1)))!=0 && candidate.getTimeInMillis()>now) return candidate.getTimeInMillis();
  }
  return 0;
 }
}
