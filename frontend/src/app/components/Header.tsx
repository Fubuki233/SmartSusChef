import React from 'react';
import { useApp } from '@/app/context/AppContext';
import { Bell, User as UserIcon, ChefHat, LogOut, Settings } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/app/components/ui/dropdown-menu';
import { Avatar, AvatarFallback, AvatarImage } from '@/app/components/ui/avatar';

interface HeaderProps {
  onNavigateToSettings?: () => void;
  showSettingsLink?: boolean;
}

export function Header({ onNavigateToSettings, showSettingsLink = true }: HeaderProps) {
  const context = useApp();
  if (!context) return null;
  const { user, logout, storeSettings } = context;

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase();
  };

  return (
    <header className="sticky top-0 z-50 w-full border-b bg-white shadow-sm h-16 flex items-center px-6 justify-between">
      {/* Left: Logo & App Name */}
      <div className="flex items-center gap-3">
        <div className="bg-[#4F6F52] p-2 rounded-[8px]">
          <ChefHat className="w-6 h-6 text-white" />
        </div>
        <span className="text-xl font-bold text-[#1A1C18] hidden md:block">
          SmartSus Chef
        </span>
      </div>

      {/* Right: Store Context, Notifications, Profile */}
      <div className="flex items-center gap-4">
        {/* Store Context Indicator */}
        {storeSettings && (
          <div className="hidden sm:flex items-center border-r pr-4 mr-2 border-gray-200 h-8">
            <span className="text-sm font-bold text-[#1A1C18]">
              {storeSettings.storeName} | {storeSettings.outletLocation}
            </span>
          </div>
        )}

        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon" className="relative text-gray-600 rounded-full h-10 w-10">
            <Bell className="w-5 h-5" />
            <span className="absolute top-2 right-2 w-2 h-2 bg-[#E74C3C] rounded-full border-2 border-white"></span>
          </Button>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="flex items-center gap-3 px-2 rounded-full hover:bg-gray-100 h-10">
                <Avatar className="h-8 w-8 border-2 border-[#4F6F52]">
                  <AvatarFallback className="bg-[#4F6F52]/10 text-[#4F6F52] text-xs font-bold">
                    {user ? getInitials(user.name) : '??'}
                  </AvatarFallback>
                </Avatar>
                <div className="hidden md:flex flex-col items-start leading-tight">
                  <span className="text-sm font-bold">{user?.name}</span>
                  <span className="text-[10px] uppercase tracking-wider text-gray-500 font-bold">{user?.role}</span>
                </div>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56 rounded-[12px] p-2">
              <DropdownMenuLabel className="font-bold">My Account</DropdownMenuLabel>
              <DropdownMenuSeparator />
              {showSettingsLink && onNavigateToSettings && (
                <DropdownMenuItem onClick={onNavigateToSettings} className="gap-2 rounded-[8px] cursor-pointer">
                  <Settings className="w-4 h-4" />
                  <span>Settings</span>
                </DropdownMenuItem>
              )}
              <DropdownMenuItem onClick={logout} className="gap-2 rounded-[8px] text-[#E74C3C] focus:text-[#E74C3C] focus:bg-red-50 cursor-pointer">
                <LogOut className="w-4 h-4" />
                <span>Log out</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </header>
  );
}
