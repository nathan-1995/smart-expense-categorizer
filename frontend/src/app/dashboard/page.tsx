'use client';

import { useState, useEffect } from 'react';
import { useToast } from '../../components/ToastProvider';
import { getApiConfig } from '../../lib/api';

interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  isEmailVerified: boolean;
}

export default function DashboardPage() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const [isResendingVerification, setIsResendingVerification] = useState(false);
  const [checkingVerification, setCheckingVerification] = useState(false);
  const { showSuccess, showInfo, showToast } = useToast();

  useEffect(() => {
    // Check if user is logged in
    const token = localStorage.getItem('token');
    const userData = localStorage.getItem('user');

    if (!token || !userData) {
      // Redirect to login if no authentication
      window.location.href = '/login';
      return;
    }

    try {
      const parsedUser = JSON.parse(userData);
      setUser(parsedUser);
      
      // Check for welcome message flag
      const welcomeData = localStorage.getItem('showWelcome');
      if (welcomeData) {
        try {
          const welcome = JSON.parse(welcomeData);
          // Show welcome message if it's fresh (within 10 seconds)
          if (welcome.show && Date.now() - welcome.timestamp < 10000) {
            showSuccess('Login successful!', `Welcome back, ${welcome.userName}!`);
          }
          // Clear the flag after use
          localStorage.removeItem('showWelcome');
        } catch (err) {
          console.error('Error parsing welcome data:', err);
          localStorage.removeItem('showWelcome');
        }
      }
    } catch (error) {
      console.error('Error parsing user data:', error);
      // Clear invalid data and redirect to login
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      localStorage.removeItem('showWelcome');
      window.location.href = '/login';
    } finally {
      setLoading(false);
    }
  }, [showSuccess]);

  // Check verification status from API
  const checkVerificationStatus = async () => {
    if (!user || checkingVerification || user.isEmailVerified) return;
    
    setCheckingVerification(true);
    
    try {
      const apiConfig = getApiConfig();
      const response = await fetch(`${apiConfig.baseUrl}/api/auth/verification-status/${user.id}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });
      
      if (response.ok) {
        const data = await response.json();
        if (data.success && data.data.isEmailVerified) {
          // Update user state
          const updatedUser = { ...user, isEmailVerified: true };
          setUser(updatedUser);
          
          // Update localStorage
          localStorage.setItem('user', JSON.stringify(updatedUser));
          
          // Show success message
          showSuccess('Email verified!', 'Your email has been successfully verified.');
        }
      }
    } catch (error) {
      console.error('Error checking verification status:', error);
    } finally {
      setCheckingVerification(false);
    }
  };

  // Poll verification status every 3 seconds if email is not verified
  useEffect(() => {
    if (!user || user.isEmailVerified) return;
    
    const interval = setInterval(() => {
      checkVerificationStatus();
    }, 3000); // Check every 3 seconds
    
    return () => clearInterval(interval);
  }, [user]);

  const handleResendVerification = async () => {
    if (!user || isResendingVerification) return;
    
    setIsResendingVerification(true);
    
    try {
      const apiConfig = getApiConfig();
      const response = await fetch(`${apiConfig.baseUrl}/api/auth/resend-verification`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email: user.email }),
      });
      
      const data = await response.json();
      showToast(data.message || 'Verification email sent', data.success ? 'success' : 'info');
    } catch (error) {
      showToast('Failed to send verification email', 'error');
    } finally {
      setIsResendingVerification(false);
    }
  };

  const handleLogout = async () => {
    if (isLoggingOut) return; // Prevent double-click
    
    setIsLoggingOut(true);
    
    // Show logout toast
    showInfo('Logging out...', 'See you again soon!');
    
    // Small delay for smooth UX
    setTimeout(() => {
      // Clear user data
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      localStorage.removeItem('showWelcome');
      localStorage.removeItem('pendingVerificationEmail');
      
      // Redirect to home page
      window.location.href = '/';
    }, 1000);
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center">
        <div className="text-white">Loading...</div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center">
        <div className="text-white">Redirecting to login...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-black text-white">
      {/* Header */}
      <header className="bg-zinc-900 border-b border-zinc-800">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            {/* Logo */}
            <div className="flex items-center">
              <h1 className="text-xl font-bold text-white">Smart Expense</h1>
            </div>

            {/* User Menu */}
            <div className="flex items-center space-x-4">
              <span className="text-gray-300">
                Welcome, {user.firstName || user.email}
              </span>
              <button
                onClick={handleLogout}
                disabled={isLoggingOut}
                className="bg-white text-black px-3 py-2 rounded text-sm hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-white focus:ring-offset-2 focus:ring-offset-black transition-all duration-200 disabled:opacity-70 disabled:cursor-not-allowed disabled:hover:bg-white"
              >
                <div className="flex items-center justify-center">
                  {isLoggingOut && (
                    <svg className="animate-spin -ml-1 mr-2 h-3 w-3 text-black" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                  )}
                  <span className={`transition-all duration-200 ${isLoggingOut ? 'opacity-75' : ''}`}>
                    {isLoggingOut ? 'Logging out...' : 'Logout'}
                  </span>
                </div>
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Email Verification Banner */}
      {!user.isEmailVerified && (
        <div className="bg-yellow-600 border-b border-yellow-500">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex items-center justify-between py-3">
              <div className="flex items-center">
                <svg className="w-5 h-5 text-yellow-100 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16c-.77.833.192 2.5 1.732 2.5z" />
                </svg>
                <span className="text-yellow-100 text-sm font-medium">
                  Please verify your email address ({user.email}) to secure your account
                </span>
              </div>
              <div className="flex space-x-2">
                <button
                  onClick={handleResendVerification}
                  disabled={isResendingVerification}
                  className="bg-yellow-500 hover:bg-yellow-400 text-yellow-900 px-3 py-1 rounded text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isResendingVerification ? 'Sending...' : 'Resend Email'}
                </button>
                <button
                  onClick={checkVerificationStatus}
                  disabled={checkingVerification}
                  className="bg-blue-500 hover:bg-blue-400 text-white px-3 py-1 rounded text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {checkingVerification ? 'Checking...' : 'Check Now'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <h2 className="text-3xl font-bold text-white mb-2">Dashboard</h2>
          <p className="text-gray-400">Manage your expenses and view insights</p>
        </div>

        {/* User Info Card */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6 mb-8">
          <h3 className="text-lg font-semibold text-white mb-4">Account Information</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-400">Name:</span>
              <span className="ml-2 text-white">
                {user.firstName && user.lastName 
                  ? `${user.firstName} ${user.lastName}` 
                  : 'Not provided'}
              </span>
            </div>
            <div>
              <span className="text-gray-400">Email:</span>
              <span className="ml-2 text-white">{user.email}</span>
            </div>
            <div>
              <span className="text-gray-400">User ID:</span>
              <span className="ml-2 text-white font-mono text-xs">{user.id}</span>
            </div>
            <div>
              <span className="text-gray-400">Email Verified:</span>
              <span className={`ml-2 ${user.isEmailVerified ? 'text-green-400' : 'text-red-400'}`}>
                {user.isEmailVerified ? 'Yes' : 'No'}
              </span>
              {!user.isEmailVerified && checkingVerification && (
                <span className="ml-2 text-blue-400 text-xs">
                  <svg className="inline w-3 h-3 animate-spin mr-1" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  checking...
                </span>
              )}
            </div>
          </div>
        </div>

        {/* Placeholder for future features */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {/* Expenses Card */}
          <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
            <h3 className="text-lg font-semibold text-white mb-2">Expenses</h3>
            <p className="text-gray-400 text-sm mb-4">Track and categorize your expenses</p>
            <button className="w-full bg-white text-black py-2 px-4 rounded hover:bg-gray-100 transition-colors">
              View Expenses
            </button>
          </div>

          {/* Categories Card */}
          <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
            <h3 className="text-lg font-semibold text-white mb-2">Categories</h3>
            <p className="text-gray-400 text-sm mb-4">Manage expense categories</p>
            <button className="w-full bg-white text-black py-2 px-4 rounded hover:bg-gray-100 transition-colors">
              Manage Categories
            </button>
          </div>

          {/* Reports Card */}
          <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
            <h3 className="text-lg font-semibold text-white mb-2">Reports</h3>
            <p className="text-gray-400 text-sm mb-4">View spending insights</p>
            <button className="w-full bg-white text-black py-2 px-4 rounded hover:bg-gray-100 transition-colors">
              View Reports
            </button>
          </div>
        </div>
      </main>
    </div>
  );
}