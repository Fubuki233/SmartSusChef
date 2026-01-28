import React, { useState, useEffect } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Badge } from '@/app/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/app/components/ui/tabs';
import { Avatar, AvatarFallback } from '@/app/components/ui/avatar';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
  DialogClose
} from '@/app/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/app/components/ui/select';
import {
  Store,
  Users,
  ShieldCheck,
  Save,
  ArrowLeft,
  Building2,
  Lock,
  UserCircle,
  Plus,
  Pencil,
  Trash2,
  MapPin,
  Phone,
  Loader2
} from 'lucide-react';
import { toast } from 'sonner';
import { User } from '@/app/types';

interface StoreSettingsProps {
  onBack?: () => void;
}

export function StoreSettings({ onBack }: StoreSettingsProps) {
  const {
    storeSettings,
    updateStoreSettings,
    storeUsers,
    user,
    addUser,
    updateUser,
    deleteUser,
    loadStoreUsers
  } = useApp();

  const isManager = user?.role === 'manager';

  const [formData, setFormData] = useState({ ...storeSettings });
  const [isSaving, setIsSaving] = useState(false);
  const [isLoadingUsers, setIsLoadingUsers] = useState(false);
  const [isSubmittingUser, setIsSubmittingUser] = useState(false);

  // User Dialog State
  const [isUserDialogOpen, setIsUserDialogOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [userForm, setUserForm] = useState({
    name: '',
    email: '',
    username: '',
    password: '',
    role: 'employee' as 'manager' | 'employee',
    status: 'Active' as 'Active' | 'Inactive'
  });

  // Load users on mount for managers
  useEffect(() => {
    if (isManager) {
      setIsLoadingUsers(true);
      loadStoreUsers().finally(() => setIsLoadingUsers(false));
    }
  }, [isManager, loadStoreUsers]);

  // Sync form when editing starts or dialog opens
  useEffect(() => {
    if (editingUser) {
      setUserForm({
        name: editingUser.name,
        email: editingUser.email || '',
        username: editingUser.username,
        password: '', // Don't pre-fill password for editing
        role: editingUser.role,
        status: editingUser.status || 'Active'
      });
    } else {
      setUserForm({
        name: '',
        email: '',
        username: '',
        password: '',
        role: 'employee',
        status: 'Active'
      });
    }
  }, [editingUser, isUserDialogOpen]);

  const handleSaveStore = async () => {
    if (!isManager) return;
    setIsSaving(true);
    try {
      await updateStoreSettings(formData);
      toast.success('Store settings updated successfully');
    } catch (error) {
      toast.error('Failed to update store settings');
    } finally {
      setIsSaving(false);
    }
  };

  const handleUserSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!userForm.name || !userForm.username) {
      toast.error('Please fill in all required fields');
      return;
    }

    // Password required for new users
    if (!editingUser && !userForm.password) {
      toast.error('Password is required for new users');
      return;
    }

    setIsSubmittingUser(true);
    try {
      if (editingUser) {
        await updateUser(editingUser.id, {
          name: userForm.name,
          email: userForm.email,
          role: userForm.role,
          status: userForm.status
        });
        toast.success('User updated successfully');
      } else {
        await addUser({
          name: userForm.name,
          email: userForm.email,
          username: userForm.username,
          password: userForm.password,
          role: userForm.role
        });
        toast.success('New user added successfully');
      }

      setIsUserDialogOpen(false);
      setEditingUser(null);
    } catch (error: any) {
      toast.error(error.message || 'Failed to save user');
    } finally {
      setIsSubmittingUser(false);
    }
  };

  const handleEditUser = (u: User) => {
    setEditingUser(u);
    setIsUserDialogOpen(true);
  };

  const handleDeleteUser = async (id: string) => {
    if (id === user?.id) {
      toast.error("You cannot delete your own account");
      return;
    }
    if (window.confirm("Are you sure you want to delete this user? This action cannot be undone.")) {
      try {
        await deleteUser(id);
        toast.success("User deleted successfully");
      } catch (error: any) {
        toast.error(error.message || "Failed to delete user");
      }
    }
  };

  return (
    <div className="space-y-6 max-w-5xl mx-auto pb-12">
      {/* Settings Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-4">
          {onBack && (
            <Button variant="ghost" size="icon" onClick={onBack} className="rounded-full">
              <ArrowLeft className="w-5 h-5" />
            </Button>
          )}
          <div>
            <h1 className="text-3xl font-bold text-[#1A1C18]">Settings</h1>
            <p className="text-gray-500">
              {isManager
                ? 'Manage store profile, team access, and security'
                : 'Manage your profile and account security'}
            </p>
          </div>
        </div>

        {isManager && (
          <Button
            onClick={handleSaveStore}
            disabled={isSaving}
            className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] px-8 gap-2 h-11 shadow-sm"
          >
            {isSaving ? 'Saving...' : <><Save className="w-4 h-4" /> Save Changes</>}
          </Button>
        )}
      </div>

      <Tabs defaultValue={isManager ? "store" : "security"} className="space-y-6">
        <TabsList className="bg-white border p-1 rounded-[12px] h-12 shadow-sm inline-flex w-full sm:w-auto">
          {isManager && (
            <>
              <TabsTrigger value="store" className="rounded-[8px] px-6 gap-2 data-[state=active]:bg-[#4F6F52] data-[state=active]:text-white transition-all h-full">
                <Store className="w-4 h-4" /> Store Profile
              </TabsTrigger>
              <TabsTrigger value="team" className="rounded-[8px] px-6 gap-2 data-[state=active]:bg-[#4F6F52] data-[state=active]:text-white transition-all h-full">
                <Users className="w-4 h-4" /> Team Access
              </TabsTrigger>
            </>
          )}
          <TabsTrigger value="security" className="rounded-[8px] px-6 gap-2 data-[state=active]:bg-[#4F6F52] data-[state=active]:text-white transition-all h-full">
            <ShieldCheck className="w-4 h-4" /> Security & Profile
          </TabsTrigger>
        </TabsList>

        {/* Manager-only Store Profile Tab */}
        {isManager && (
          <TabsContent value="store" className="space-y-6">
            <Card className="rounded-[8px] border-gray-200 overflow-hidden shadow-sm">
              <CardHeader className="bg-gray-50/50 border-b">
                <CardTitle className="text-xl flex items-center gap-2 text-[#1A1C18]">
                  <Building2 className="w-5 h-5 text-[#4F6F52]" />
                  Store Profile Management
                </CardTitle>
                <CardDescription>Update corporate and operational identity for this outlet</CardDescription>
              </CardHeader>
              <CardContent className="p-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                  <div className="space-y-6">
                    <div className="space-y-2">
                      <Label className="text-sm font-bold text-gray-700">StoreID (System-generated)</Label>
                      <Input value={formData.storeId} readOnly className="bg-gray-50 border-gray-200 rounded-[8px] font-mono text-xs text-gray-500 cursor-not-allowed" />
                    </div>
                    <div className="space-y-2">
                      <Label className="text-sm font-bold text-gray-700">Company Name</Label>
                      <Input value={formData.companyName} onChange={(e) => setFormData({ ...formData, companyName: e.target.value })} className="rounded-[8px] border-gray-200" />
                    </div>
                    <div className="space-y-2">
                      <Label className="text-sm font-bold text-gray-700">UEN (Unique Entity Number)</Label>
                      <Input value={formData.uen} onChange={(e) => setFormData({ ...formData, uen: e.target.value })} className="rounded-[8px] border-gray-200" />
                    </div>
                  </div>
                  <div className="space-y-6">
                    <div className="space-y-2">
                      <Label className="text-sm font-bold text-gray-700">Store Name</Label>
                      <Input value={formData.storeName} onChange={(e) => setFormData({ ...formData, storeName: e.target.value })} className="rounded-[8px] border-gray-200" />
                    </div>
                    <div className="space-y-2">
                      <Label className="text-sm font-bold text-gray-700">Outlet Location (Optional)</Label>
                      <div className="relative">
                        <MapPin className="absolute left-3 top-3 w-4 h-4 text-gray-400" />
                        <Input value={formData.outletLocation} onChange={(e) => setFormData({ ...formData, outletLocation: e.target.value })} className="pl-10 rounded-[8px] border-gray-200" />
                      </div>
                    </div>
                    <div className="space-y-2">
                      <Label className="text-sm font-bold text-gray-700">Contact Number</Label>
                      <div className="relative">
                        <Phone className="absolute left-3 top-3 w-4 h-4 text-gray-400" />
                        <Input value={formData.contactNumber} onChange={(e) => setFormData({ ...formData, contactNumber: e.target.value })} className="pl-10 rounded-[8px] border-gray-200" />
                      </div>
                    </div>
                  </div>
                  <div className="md:col-span-2 space-y-2">
                    <Label className="text-sm font-bold text-gray-700">Store Address</Label>
                    <Input value={formData.address} onChange={(e) => setFormData({ ...formData, address: e.target.value })} className="rounded-[8px] border-gray-200" />
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        )}

        {/* Manager-only Team Access Tab */}
        {isManager && (
          <TabsContent value="team" className="space-y-6">
            <Card className="rounded-[8px] border-gray-200 overflow-hidden shadow-sm">
              <CardHeader className="bg-gray-50/50 border-b flex flex-row items-center justify-between p-6">
                <div>
                  <CardTitle className="text-xl flex items-center gap-2 text-[#1A1C18]">
                    <Users className="w-5 h-5 text-[#4F6F52]" />
                    Team Access Control
                  </CardTitle>
                  <CardDescription>Manage user permissions for your team members</CardDescription>
                </div>
                <Button
                  onClick={() => {
                    setEditingUser(null);
                    setIsUserDialogOpen(true);
                  }}
                  className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] gap-2 px-6"
                >
                  <Plus className="w-4 h-4" /> Add New User
                </Button>
              </CardHeader>
              <CardContent className="p-0">
                {isLoadingUsers ? (
                  <div className="flex items-center justify-center py-12">
                    <Loader2 className="w-6 h-6 animate-spin text-[#4F6F52]" />
                    <span className="ml-2 text-gray-500">Loading team members...</span>
                  </div>
                ) : storeUsers.length === 0 ? (
                  <div className="flex flex-col items-center justify-center py-12 text-gray-500">
                    <Users className="w-12 h-12 text-gray-300 mb-2" />
                    <p>No team members yet</p>
                    <p className="text-sm">Click "Add New User" to invite your first team member</p>
                  </div>
                ) : (
                  <Table>
                    <TableHeader>
                      <TableRow className="bg-gray-50/50">
                        <TableHead className="font-bold py-4 pl-6">User Info</TableHead>
                        <TableHead className="font-bold py-4">Role</TableHead>
                        <TableHead className="font-bold py-4">Status</TableHead>
                        <TableHead className="font-bold py-4 text-right pr-6">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {storeUsers.map((u) => (
                        <TableRow key={u.id} className="hover:bg-gray-50/30">
                          <TableCell className="py-4 pl-6">
                            <div className="flex items-center gap-3">
                              <Avatar className="h-9 w-9">
                                <AvatarFallback className="bg-[#4F6F52]/10 text-[#4F6F52] text-xs font-bold">
                                  {u.name.split(' ').map(n => n[0]).join('')}
                                </AvatarFallback>
                              </Avatar>
                              <div>
                                <p className="font-medium text-sm">{u.name}</p>
                                <p className="text-[11px] text-[#4F6F52] font-mono">@{u.username}</p>
                                <p className="text-[11px] text-gray-400">{u.email}</p>
                              </div>
                            </div>
                          </TableCell>
                          <TableCell>
                            <Badge variant="outline" className={`uppercase text-[10px] ${u.role === 'manager' ? 'border-[#4F6F52] text-[#4F6F52]' : ''}`}>
                              {u.role}
                            </Badge>
                          </TableCell>
                          <TableCell>
                            <Badge variant="secondary" className={`text-[10px] ${u.status === 'Active' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'}`}>
                              {u.status || 'Active'}
                            </Badge>
                          </TableCell>
                          <TableCell className="text-right pr-6">
                            <div className="flex justify-end gap-2">
                              <Button variant="ghost" size="icon" onClick={() => handleEditUser(u)} className="h-8 w-8 rounded-full hover:bg-[#4F6F52]/10">
                                <Pencil className="w-4 h-4" />
                              </Button>
                              <Button variant="ghost" size="icon" onClick={() => handleDeleteUser(u.id)} className="h-8 w-8 rounded-full hover:bg-red-50 hover:text-red-600" disabled={u.id === user?.id}>
                                <Trash2 className="w-4 h-4" />
                              </Button>
                            </div>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </CardContent>
            </Card>
          </TabsContent>
        )}

        {/* Universal Security & Personal Profile Tab */}
        <TabsContent value="security" className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Card className="rounded-[8px] border-gray-200 overflow-hidden shadow-sm">
              <CardHeader className="bg-gray-50/50 border-b">
                <CardTitle className="text-xl flex items-center gap-2 text-[#1A1C18]">
                  <Lock className="w-5 h-5 text-[#4F6F52]" />
                  Password Management
                </CardTitle>
                <CardDescription>Securely change your account password</CardDescription>
              </CardHeader>
              <CardContent className="p-6 space-y-4">
                <div className="space-y-2">
                  <Label className="text-sm font-bold text-gray-700">Current Password</Label>
                  <Input type="password" placeholder="••••••••" className="rounded-[8px] border-gray-200" />
                </div>
                <div className="space-y-2">
                  <Label className="text-sm font-bold text-gray-700">New Password</Label>
                  <Input type="password" placeholder="••••••••" className="rounded-[8px] border-gray-200" />
                </div>
                <Button className="w-full bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] h-11 shadow-sm mt-2">
                  Update Password
                </Button>
              </CardContent>
            </Card>

            <Card className="rounded-[8px] border-gray-200 overflow-hidden shadow-sm">
              <CardHeader className="bg-gray-50/50 border-b">
                <CardTitle className="text-xl flex items-center gap-2 text-[#1A1C18]">
                  <UserCircle className="w-5 h-5 text-[#4F6F52]" />
                  Personal Profile
                </CardTitle>
                <CardDescription>Update your personal contact information</CardDescription>
              </CardHeader>
              <CardContent className="p-6 space-y-6">
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label className="text-sm font-bold text-gray-700">Full Name</Label>
                    <Input defaultValue={user?.name} className="rounded-[8px] border-gray-200" />
                  </div>
                  <div className="space-y-2">
                    <Label className="text-sm font-bold text-gray-700">Email Address</Label>
                    <Input defaultValue={user?.email} className="rounded-[8px] border-gray-200" />
                  </div>
                </div>
                <Button variant="outline" className="w-full border-[#4F6F52] text-[#4F6F52] hover:bg-[#4F6F52]/5 rounded-[32px] h-11">
                  Save Profile Info
                </Button>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>

      {/* User Management Dialog (Add/Edit) */}
      <Dialog open={isUserDialogOpen} onOpenChange={setIsUserDialogOpen}>
        <DialogContent className="sm:max-w-[425px] rounded-[12px] border-none shadow-2xl">
          <DialogHeader>
            <DialogTitle className="text-xl text-[#1A1C18]">
              {editingUser ? 'Edit User Details' : 'Add New Team Member'}
            </DialogTitle>
            <DialogDescription>
              {editingUser
                ? "Update this member's profile and outlet permissions."
                : "Enter details below to invite a new user to this store."}
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={handleUserSubmit} className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="username" className="text-sm font-bold flex justify-between">
                Username
                <span className="text-[10px] font-normal text-gray-400 uppercase tracking-widest">
                  {editingUser ? "Permanent ID" : "Unique Identifier"}
                </span>
              </Label>
              <Input
                id="username"
                placeholder="e.g. jason_tan88"
                value={userForm.username}
                onChange={(e) => setUserForm({ ...userForm, username: e.target.value })}
                className={`rounded-[8px] ${editingUser ? 'bg-gray-50 cursor-not-allowed opacity-80 italic' : ''}`}
                required
                disabled={!!editingUser}
              />
              {/* This paragraph now shows in both Add and Edit modes */}
              <p className="text-[10px] text-gray-400 italic px-1">
                {editingUser
                  ? "Usernames are permanent and cannot be modified."
                  : "Note: This username is permanent and cannot be changed once created."}
              </p>
            </div>

            {/* Password field - only for new users */}
            {!editingUser && (
              <div className="space-y-2">
                <Label htmlFor="password" className="text-sm font-bold">Password</Label>
                <Input
                  id="password"
                  type="password"
                  placeholder="Create a secure password"
                  value={userForm.password}
                  onChange={(e) => setUserForm({ ...userForm, password: e.target.value })}
                  className="rounded-[8px]"
                  required
                  minLength={6}
                />
                <p className="text-[10px] text-gray-400 italic px-1">
                  Minimum 6 characters. User can change password after first login.
                </p>
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="name" className="text-sm font-bold">Full Name</Label>
              <Input
                id="name"
                placeholder="e.g. Tan Ah Kow"
                value={userForm.name}
                onChange={(e) => setUserForm({ ...userForm, name: e.target.value })}
                className="rounded-[8px]"
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="email" className="text-sm font-bold">Email Address</Label>
              <Input
                id="email"
                type="email"
                placeholder="name@example.com"
                value={userForm.email}
                onChange={(e) => setUserForm({ ...userForm, email: e.target.value })}
                className="rounded-[8px]"
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label className="text-sm font-bold">Role</Label>
                <Select
                  value={userForm.role}
                  onValueChange={(value: any) => setUserForm({ ...userForm, role: value })}
                >
                  <SelectTrigger className="rounded-[8px]">
                    <SelectValue placeholder="Select role" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="manager">Manager</SelectItem>
                    <SelectItem value="employee">Employee</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label className="text-sm font-bold">Status</Label>
                <Select
                  value={userForm.status}
                  onValueChange={(value: any) => setUserForm({ ...userForm, status: value })}
                >
                  <SelectTrigger className="rounded-[8px]">
                    <SelectValue placeholder="Select status" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">Active</SelectItem>
                    <SelectItem value="Inactive">Inactive</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <DialogFooter className="pt-6 gap-2">
              <DialogClose asChild>
                <Button type="button" variant="outline" className="rounded-[32px] flex-1" disabled={isSubmittingUser}>
                  Cancel
                </Button>
              </DialogClose>
              <Button
                type="submit"
                className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] flex-1"
                disabled={isSubmittingUser}
              >
                {isSubmittingUser ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    {editingUser ? 'Saving...' : 'Creating...'}
                  </>
                ) : (
                  editingUser ? 'Save Changes' : 'Create Account'
                )}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}