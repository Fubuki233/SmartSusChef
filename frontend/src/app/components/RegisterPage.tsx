import React, { useState } from 'react';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/app/components/ui/card';
import { ChefHat, AlertCircle, ArrowLeft, CheckCircle2, Loader2 } from 'lucide-react';
import { api } from '@/app/services/api';
import { toast } from 'sonner';

interface RegisterPageProps {
    onRegisterSuccess: (userData: any, storeId: number) => void;
    onBackToLogin: () => void;
}

export function RegisterPage({ onRegisterSuccess, onBackToLogin }: RegisterPageProps) {
    const [formData, setFormData] = useState({
        username: '',
        password: '',
        confirmPassword: '',
        name: '',
        email: '',
    });
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
        setError('');
    };

    const validateForm = (): boolean => {
        if (!formData.username.trim()) {
            setError('Username is required');
            return false;
        }
        if (formData.username.length < 3) {
            setError('Username must be at least 3 characters');
            return false;
        }
        if (!formData.password) {
            setError('Password is required');
            return false;
        }
        if (formData.password.length < 6) {
            setError('Password must be at least 6 characters');
            return false;
        }
        if (formData.password !== formData.confirmPassword) {
            setError('Passwords do not match');
            return false;
        }
        if (!formData.name.trim()) {
            setError('Full name is required');
            return false;
        }
        if (!formData.email.trim()) {
            setError('Email is required');
            return false;
        }
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
            setError('Please enter a valid email address');
            return false;
        }
        return true;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');

        if (!validateForm()) {
            return;
        }

        setIsLoading(true);
        try {
            const response = await api.auth.register({
                username: formData.username,
                password: formData.password,
                name: formData.name,
                email: formData.email,
            });

            toast.success('Account created successfully!');
            onRegisterSuccess(response.user, response.storeId);
        } catch (err: any) {
            const message = err.message || 'Registration failed. Please try again.';
            setError(message);
            toast.error(message);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="min-h-screen flex items-center justify-center bg-[#F9FBF7] p-4">
            <Card className="w-full max-w-md shadow-lg rounded-[8px]">
                <CardHeader className="space-y-4 text-center">
                    <div className="flex justify-center">
                        <div className="bg-[#4F6F52] p-4 rounded-full shadow-md">
                            <ChefHat className="w-12 h-12 text-white" />
                        </div>
                    </div>
                    <CardTitle className="text-3xl text-[#1A1C18]">Create Account</CardTitle>
                    <CardDescription className="text-[#6b7280]">
                        Register as a Manager to set up your restaurant
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit} className="space-y-4">
                        {error && (
                            <div className="flex items-center gap-2 p-3 bg-red-50 text-red-700 rounded-lg text-sm">
                                <AlertCircle className="w-4 h-4 flex-shrink-0" />
                                <span>{error}</span>
                            </div>
                        )}

                        <div className="space-y-2">
                            <Label htmlFor="name">Full Name *</Label>
                            <Input
                                id="name"
                                name="name"
                                type="text"
                                placeholder="Enter your full name"
                                value={formData.name}
                                onChange={handleChange}
                                required
                                className="rounded-[8px]"
                                disabled={isLoading}
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="email">Email Address *</Label>
                            <Input
                                id="email"
                                name="email"
                                type="email"
                                placeholder="Enter your email"
                                value={formData.email}
                                onChange={handleChange}
                                required
                                className="rounded-[8px]"
                                disabled={isLoading}
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="username">Username *</Label>
                            <Input
                                id="username"
                                name="username"
                                type="text"
                                placeholder="Choose a username"
                                value={formData.username}
                                onChange={handleChange}
                                required
                                className="rounded-[8px]"
                                disabled={isLoading}
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="password">Password *</Label>
                            <Input
                                id="password"
                                name="password"
                                type="password"
                                placeholder="Create a password (min. 6 characters)"
                                value={formData.password}
                                onChange={handleChange}
                                required
                                className="rounded-[8px]"
                                disabled={isLoading}
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="confirmPassword">Confirm Password *</Label>
                            <Input
                                id="confirmPassword"
                                name="confirmPassword"
                                type="password"
                                placeholder="Confirm your password"
                                value={formData.confirmPassword}
                                onChange={handleChange}
                                required
                                className="rounded-[8px]"
                                disabled={isLoading}
                            />
                        </div>

                        <div className="bg-blue-50 p-3 rounded-lg text-sm text-blue-700">
                            <div className="flex items-start gap-2">
                                <CheckCircle2 className="w-4 h-4 mt-0.5 flex-shrink-0" />
                                <span>
                                    As a manager, you'll be able to set up your store, manage employees,
                                    and access all features of SmartSus Chef.
                                </span>
                            </div>
                        </div>

                        <Button
                            type="submit"
                            className="w-full bg-[#4F6F52] hover:bg-[#3d5a40] text-white rounded-[32px] h-11"
                            disabled={isLoading}
                        >
                            {isLoading ? (
                                <>
                                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                                    Creating Account...
                                </>
                            ) : (
                                'Create Manager Account'
                            )}
                        </Button>

                        <div className="text-center pt-2">
                            <Button
                                type="button"
                                variant="ghost"
                                onClick={onBackToLogin}
                                className="text-[#4F6F52] hover:text-[#3d5a40]"
                                disabled={isLoading}
                            >
                                <ArrowLeft className="w-4 h-4 mr-2" />
                                Back to Login
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}
