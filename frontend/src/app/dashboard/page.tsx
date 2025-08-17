'use client';

import { useState, useEffect } from 'react';
import { useToast } from '../../components/ToastProvider';

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
  const { showSuccess } = useToast();

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

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    localStorage.removeItem('showWelcome');
    window.location.href = '/';
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
                className="bg-white text-black px-3 py-1 rounded text-sm hover:bg-gray-100 transition-colors"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </header>

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