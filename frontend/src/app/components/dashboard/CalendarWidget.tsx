import React from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle } from '@/app/components/ui/card';
import { Calendar as CalendarIcon, Loader2 } from 'lucide-react';
import { format, parseISO, isAfter, isBefore, addDays } from 'date-fns';

export function CalendarWidget() {
  const { holidays, dataLoading } = useApp();
  const today = new Date();
  const nextWeek = addDays(today, 7);

  const upcomingHolidays = holidays
    .filter((holiday) => {
      const holidayDate = parseISO(holiday.date);
      return (
        (isAfter(holidayDate, today) || holidayDate.toDateString() === today.toDateString()) &&
        isBefore(holidayDate, addDays(today, 60))
      );
    })
    .slice(0, 3);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <CalendarIcon className="w-4 h-4" />
          Upcoming Events
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-2">
          {dataLoading ? (
            <div className="flex items-center justify-center py-4">
              <Loader2 className="w-5 h-5 animate-spin text-gray-400" />
              <span className="ml-2 text-sm text-gray-500">Loading events...</span>
            </div>
          ) : upcomingHolidays.length > 0 ? (
            upcomingHolidays.map((event) => {
              const eventDate = parseISO(event.date);
              return (
                <div
                  key={event.date}
                  className="flex items-center gap-3 p-3 bg-[#E6EFE0] rounded-lg hover:bg-[#ECE3CE] transition-colors"
                >
                  <div className="bg-[#4F6F52] text-white rounded-lg p-2 min-w-[56px] text-center shadow-sm">
                    <div className="text-xs font-medium">{format(eventDate, 'MMM')}</div>
                    <div className="text-xl font-bold">{format(eventDate, 'd')}</div>
                  </div>
                  <div className="flex-1">
                    <p className="font-medium text-[#1A1C18]">{event.name}</p>
                    <p className="text-sm text-gray-600">{format(eventDate, 'EEEE')}</p>
                  </div>
                </div>
              );
            })
          ) : (
            <p className="text-sm text-gray-500 text-center py-4">No upcoming events</p>
          )}
        </div>
      </CardContent>
    </Card>
  );
}