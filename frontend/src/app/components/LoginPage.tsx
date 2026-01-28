import React, { useState } from 'react';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/app/components/ui/card';
import { ChefHat, AlertCircle, ArrowLeft, MailCheck, UserPlus } from 'lucide-react';
import { useApp } from '@/app/context/AppContext';
import { toast } from 'sonner';

interface LoginPageProps {
  onNavigateToRegister?: () => void;
}

export function LoginPage({ onNavigateToRegister }: LoginPageProps) {
  const context = useApp();
  if (!context) return null;
  const { login } = context;
  const [view, setView] = useState<'login' | 'forgot-password' | 'success'>('login');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);
    try {
      const result = await login(username, password);
      if (!result.success) {
        setError('Invalid credentials. Please key in the correct username and password.');
        toast.error('Login failed');
      } else {
        toast.success('Login successful!');
      }
    } catch (err) {
      setError('An error occurred during login. Please try again.');
      toast.error('Login error');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleResetRequest = (e: React.FormEvent) => {
    e.preventDefault();
    // Feature 18B logic
    // Simulate sending email
    setView('success');
    toast.success(`Reset link sent to ${email}`);
  };

  /* --- View 1: Success Message --- */
  if (view === 'success') {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[#F9FBF7] p-4">
        <Card className="w-full max-w-md text-center shadow-lg rounded-[8px]">
          <CardContent className="pt-10 pb-10 space-y-6">
            <div className="flex justify-center">
              <div className="bg-green-100 p-4 rounded-full">
                <MailCheck className="w-12 h-12 text-[#4F6F52]" />
              </div>
            </div>
            <div className="space-y-2">
              <h2 className="text-2xl font-bold text-[#1A1C18]">Check your email</h2>
              <p className="text-gray-500">We've sent a password reset link to <strong>{email}</strong></p>
            </div>
            <Button onClick={() => setView('login')} variant="outline" className="w-full rounded-[32px] border-[#4F6F52] text-[#4F6F52]">
              Return to Login
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-[#F9FBF7] p-4">
      <Card className="w-full max-w-md shadow-lg rounded-[8px]">
        <CardHeader className="space-y-4 text-center">
          <div className="flex justify-center">
            <div className="bg-[#4F6F52] p-4 rounded-full shadow-md">
              <ChefHat className="w-12 h-12 text-white" />
            </div>
          </div>
          <CardTitle className="text-3xl text-[#1A1C18]">SmartSus Chef</CardTitle>
          <CardDescription className="text-[#6b7280]">
            {view === 'login' ? 'Demand Forecasting & Food Prep Recommendation' : 'Request Password Reset'}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {view === 'login' ? (
            /* --- View 2: Login Form --- */
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="username">Username</Label>
                <Input
                  id="username"
                  type="text"
                  placeholder="Enter your username"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  required
                  className="rounded-[8px]"
                />
              </div>
              <div className="space-y-2">
                <div className="flex justify-between items-center">
                  <Label htmlFor="password">Password</Label>
                  {/* Password Reset Trigger */}
                  <button
                    type="button"
                    onClick={() => setView('forgot-password')}
                    className="text-xs text-[#4F6F52] hover:underline font-medium"
                  >
                    Forgot Password?
                  </button>
                </div>
                <Input
                  id="password"
                  type="password"
                  placeholder="Enter your password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  className="rounded-[8px]"
                />
              </div>

              {error && (
                <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-lg">
                  <AlertCircle className="w-5 h-5 text-red-600 flex-shrink-0 mt-0.5" />
                  <p className="text-sm text-red-800">{error}</p>
                </div>
              )}

              <Button
                type="submit"
                disabled={isLoading}
                className="bg-[#4F6F52] hover:bg-[#3d563f] text-white w-full rounded-[32px]"
              >
                {isLoading ? 'Signing In...' : 'Sign In'}
              </Button>

              {onNavigateToRegister && (
                <div className="pt-4 border-t">
                  <p className="text-sm text-gray-600 text-center mb-3">New to SmartSus Chef?</p>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={onNavigateToRegister}
                    className="w-full rounded-[32px] border-[#4F6F52] text-[#4F6F52] hover:bg-[#4F6F52] hover:text-white"
                  >
                    <UserPlus className="w-4 h-4 mr-2" />
                    Create Manager Account
                  </Button>
                </div>
              )}
            </form>
          ) : (
            /* --- View 3: Forgot Password Form --- */
            <form onSubmit={handleResetRequest} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="reset-email">Email Address</Label>
                <Input
                  id="reset-email"
                  type="email"
                  placeholder="Enter your registered email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  className="rounded-[8px]"
                />
              </div>
              <Button type="submit" className="bg-[#4F6F52] hover:bg-[#3d563f] text-white w-full rounded-[32px]">
                Send Reset Link
              </Button>
              <button
                type="button"
                onClick={() => setView('login')}
                className="flex items-center justify-center gap-2 w-full text-sm text-gray-500 hover:text-gray-800 transition-colors"
              >
                <ArrowLeft className="w-4 h-4" /> Back to Login
              </button>
            </form>
          )}
        </CardContent>
      </Card>
    </div>
  );
}