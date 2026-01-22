import React from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle } from '@/app/components/ui/card';
import { Calendar as CalendarIcon } from 'lucide-react';
import { format, parseISO, isAfter, isBefore, addDays } from 'date-fns';

export function CalendarWidget() {
  const { holidays } = useApp();
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
        {upcomingHolidays.length > 0 ? (
          <div className="space-y-3">
            {upcomingHolidays.map((holiday, index) => (
              <div
                key={index}
                className="flex items-start gap-3 p-3 bg-blue-50 border border-blue-200 rounded-lg"
              >
                <div className="bg-blue-600 text-white rounded-lg p-2 text-center min-w-[50px]">
                  <div className="text-xs font-medium">
                    {format(parseISO(holiday.date), 'MMM')}
                  </div>
                  <div className="text-lg font-bold">
                    {format(parseISO(holiday.date), 'd')}
                  </div>
                </div>
                <div className="flex-1 pt-1">
                  <p className="font-medium text-sm">{holiday.name}</p>
                  <p className="text-xs text-gray-600">
                    {format(parseISO(holiday.date), 'EEEE')}
                  </p>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-gray-500">No upcoming holidays in the next 60 days</p>
        )}
      </CardContent>
    </Card>
  );
}
