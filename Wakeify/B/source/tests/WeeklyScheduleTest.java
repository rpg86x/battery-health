package com.wakelight;
import java.util.*;
public class WeeklyScheduleTest {
 static final TimeZone ZONE=TimeZone.getTimeZone("Europe/Amsterdam");
 static long date(int month,int day,int hour,int minute){Calendar c=Calendar.getInstance(ZONE);c.clear();c.set(2026,month-1,day,hour,minute);return c.getTimeInMillis();}
 static void equal(long expected,long actual){if(expected!=actual)throw new AssertionError("Unexpected schedule: "+new Date(actual));}
 public static void main(String[]args){
  equal(date(10,15,7,15),WeeklySchedule.next(date(10,14,22,0),30,7,15,ZONE));
  equal(date(10,19,7,15),WeeklySchedule.next(date(10,15,7,15),30,7,15,ZONE));
  equal(date(10,19,7,15),WeeklySchedule.next(date(10,17,8,0),30,7,15,ZONE));
  equal(date(10,16,8,30),WeeklySchedule.next(date(10,15,8,0),1<<(Calendar.FRIDAY-1),8,30,ZONE));
  equal(0,WeeklySchedule.next(date(10,14,22,0),0,7,15,ZONE));
  equal(date(10,26,7,15),WeeklySchedule.next(date(10,22,8,0),30,7,15,ZONE));
  equal(date(10,15,7,15),WeeklySchedule.next(date(10,14,22,0),62,7,15,8,0,ZONE));
  equal(date(10,16,8,0),WeeklySchedule.next(date(10,15,7,15),62,7,15,8,0,ZONE));
  equal(date(10,16,8,0),WeeklySchedule.next(date(10,16,7,30),62,7,15,8,0,ZONE));
  equal(date(10,19,7,15),WeeklySchedule.next(date(10,16,8,0),62,7,15,8,0,ZONE));
  equal(date(10,18,9,0),WeeklySchedule.next(date(10,16,8,0),63,7,15,8,0,9,0,ZONE));
  equal(date(10,18,9,0),WeeklySchedule.next(date(10,17,22,0),63,7,15,8,0,9,0,ZONE));
  equal(date(10,19,7,15),WeeklySchedule.next(date(10,18,9,0),63,7,15,8,0,9,0,ZONE));
  System.out.println("13 schedule tests passed: Mon-Thu 07:15, Friday 08:00, Sunday 09:00, Saturday off and rollover.");
 }
}
