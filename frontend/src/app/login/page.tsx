'use client';

import { useState } from 'react';
import { useToast } from '../../components/ToastProvider';
import { getAuthUrl, getHeaders } from '../../lib/api';

interface LoginResponse {
  success: boolean;
  data?: {
    token: string;
    user: {
      id: string;
      email: string;
      firstName?: string;
      lastName?: string;
    };
    expiresAt: string;
  };
  message?: string;
  errors?: string[];
}

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const { showSuccess, showError } = useToast();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    
    try {
      console.log('Attempting login with:', { email, password });
      
      const response = await fetch(getAuthUrl('/login'), {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ email, password }),
      });

      console.log('Response status:', response.status);
      console.log('Response headers:', Object.fromEntries(response.headers.entries()));

      const data: LoginResponse = await response.json();
      console.log('Response data:', data);

      if (data.success && data.data) {
        console.log('Login successful, token:', data.data.token);
        
        // Store token and user data
        localStorage.setItem('token', data.data.token);
        localStorage.setItem('user', JSON.stringify(data.data.user));
        
        // Set flag for welcome message on dashboard
        const userName = data.data.user.firstName || data.data.user.email.split('@')[0];
        localStorage.setItem('showWelcome', JSON.stringify({
          show: true,
          userName,
          timestamp: Date.now()
        }));
        
        // Redirect based on user role
        if (data.data.user.role === 'Admin') {
          window.location.href = '/admin';
        } else {
          window.location.href = '/dashboard';
        }
      } else {
        showError('Login failed', data.message || 'Please check your credentials and try again.');
        console.error('Login failed:', data);
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Network error - API Gateway might be down';
      showError('Connection error', errorMessage);
      console.error('Login error:', err);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-black flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        {/* Logo/Brand */}
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-white mb-2">
            Smart Expense
          </h1>
          <p className="text-gray-400 text-sm">
            Categorize your expenses intelligently
          </p>
        </div>

        {/* Login Form */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-8">
          <h2 className="text-xl font-semibold text-white mb-6 text-center">
            Sign in to your account
          </h2>

          
          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Email Field */}
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-300 mb-2">
                Email
              </label>
              <input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                className="w-full px-3 py-2 bg-black border border-zinc-700 rounded-md text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-white focus:border-transparent transition-all duration-200"
                placeholder="Enter your email"
              />
            </div>

            {/* Password Field */}
            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-300 mb-2">
                Password
              </label>
              <input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                className="w-full px-3 py-2 bg-black border border-zinc-700 rounded-md text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-white focus:border-transparent transition-all duration-200"
                placeholder="Enter your password"
              />
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              disabled={isLoading}
              className="w-full bg-white text-black font-medium py-3 px-4 rounded-md hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-white focus:ring-offset-2 focus:ring-offset-black transition-all duration-200 disabled:opacity-70 disabled:cursor-not-allowed disabled:hover:bg-white"
            >
              <div className="flex items-center justify-center">
                {isLoading && (
                  <svg className="animate-spin -ml-1 mr-3 h-4 w-4 text-black" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                )}
                <span className={`transition-all duration-200 ${isLoading ? 'opacity-75' : ''}`}>
                  {isLoading ? 'Signing in...' : 'Sign in'}
                </span>
              </div>
            </button>
          </form>

          {/* Divider */}
          <div className="mt-6 flex items-center">
            <div className="flex-1 border-t border-zinc-700" />
            <span className="px-4 text-sm text-gray-500">or</span>
            <div className="flex-1 border-t border-zinc-700" />
          </div>

          {/* OAuth Button */}
          <button
            type="button"
            className="w-full mt-4 bg-transparent border border-zinc-700 text-white font-medium py-2 px-4 rounded-md hover:bg-zinc-800 focus:outline-none focus:ring-2 focus:ring-white focus:ring-offset-2 focus:ring-offset-black transition-all duration-200"
          >
            Continue with Google
          </button>

          {/* Footer Links */}
          <div className="mt-6 text-center space-y-2">
            <p className="text-sm text-gray-500">
              Don't have an account?{' '}
              <a href="/register" className="text-white hover:underline">
                Sign up
              </a>
            </p>
            <p className="text-sm text-gray-500">
              Administrator?{' '}
              <a href="/admin" className="text-red-400 hover:text-red-300 transition-colors">
                Admin Portal
              </a>
            </p>
          </div>
        </div>

        {/* Terms */}
        <p className="mt-8 text-center text-xs text-gray-500">
          By signing in, you agree to our{' '}
          <a href="#" className="hover:text-gray-300 transition-colors">
            Terms of Service
          </a>{' '}
          and{' '}
          <a href="#" className="hover:text-gray-300 transition-colors">
            Privacy Policy
          </a>
        </p>
      </div>
    </div>
  );
}