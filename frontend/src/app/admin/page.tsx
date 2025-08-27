'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { useToast } from '@/components/ToastProvider';
import { getApiConfig } from '@/lib/api';

interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  isEmailVerified: boolean;
  role: string;
  lastSeenAt?: string;
  createdAt: string;
  transactionCount: number;
  categoryCount: number;
}

interface SystemStats {
  totalUsers: number;
  adminUsers: number;
  verifiedUsers: number;
  totalTransactions: number;
  totalCategories: number;
  totalBudgets: number;
  recentRegistrations: number;
  activeUsers: number;
}

export default function AdminPage() {
  const [currentUser, setCurrentUser] = useState<any>(null);
  const [users, setUsers] = useState<User[]>([]);
  const [stats, setStats] = useState<SystemStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const [currentTime, setCurrentTime] = useState(new Date());
  const [showAdminLogin, setShowAdminLogin] = useState(false);
  const [loginLoading, setLoginLoading] = useState(false);
  const [loginForm, setLoginForm] = useState({ email: '', password: '' });
  const [lastUpdated, setLastUpdated] = useState(new Date());
  const { showSuccess, showError, showToast } = useToast();

  useEffect(() => {
    // Check if user is logged in and is admin
    const token = localStorage.getItem('token');
    const userData = localStorage.getItem('user');

    if (!token || !userData) {
      setShowAdminLogin(true);
      setLoading(false);
      return;
    }

    try {
      const user = JSON.parse(userData);
      if (user.role !== 'Admin') {
        showError('Access denied', 'Admin privileges required');
        setShowAdminLogin(true);
        setLoading(false);
        return;
      }
      setCurrentUser(user);
      loadData();
    } catch (error) {
      console.error('Error parsing user data:', error);
      setShowAdminLogin(true);
      setLoading(false);
    }
  }, []);

  // Update system time every second
  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentTime(new Date());
    }, 1000);

    return () => clearInterval(timer);
  }, []);

  // Real-time data polling when admin is active
  useEffect(() => {
    if (!currentUser || showAdminLogin) return;

    const pollInterval = setInterval(async () => {
      // Only poll if tab is active (performance optimization)
      if (!document.hidden) {
        try {
          await Promise.all([loadUsers(), loadStats()]);
          setLastUpdated(new Date());
        } catch (error) {
          console.error('Error during polling update:', error);
        }
      }
    }, 60000); // Poll every 30 seconds

    return () => clearInterval(pollInterval);
  }, [currentUser, showAdminLogin]);

  const loadData = async () => {
    try {
      await Promise.all([loadUsers(), loadStats()]);
    } catch (error) {
      console.error('Error loading admin data:', error);
      showError('Error', 'Failed to load admin data');
    } finally {
      setLoading(false);
    }
  };

  const loadUsers = async () => {
    try {
      const apiConfig = getApiConfig();
      const token = localStorage.getItem('token');
      
      const response = await fetch(`${apiConfig.baseUrl}/api/admin/users`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const data = await response.json();
        setUsers(data.data || []);
      } else {
        throw new Error('Failed to load users');
      }
    } catch (error) {
      console.error('Error loading users:', error);
      showError('Error', 'Failed to load users');
    }
  };

  const loadStats = async () => {
    try {
      const apiConfig = getApiConfig();
      const token = localStorage.getItem('token');
      
      const response = await fetch(`${apiConfig.baseUrl}/api/admin/stats`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const data = await response.json();
        setStats(data.data);
      } else {
        throw new Error('Failed to load stats');
      }
    } catch (error) {
      console.error('Error loading stats:', error);
      showError('Error', 'Failed to load system statistics');
    }
  };

  const deleteUser = async (userId: string, userEmail: string) => {
    if (!confirm(`Are you sure you want to delete user ${userEmail}? This action cannot be undone and will delete all their data.`)) {
      return;
    }

    try {
      const apiConfig = getApiConfig();
      const token = localStorage.getItem('token');
      
      const response = await fetch(`${apiConfig.baseUrl}/api/admin/users/${userId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        showSuccess('User deleted', 'User and all associated data have been deleted');
        loadUsers(); // Reload the users list
        loadStats(); // Reload stats
      } else {
        const data = await response.json();
        showError('Error', data.message || 'Failed to delete user');
      }
    } catch (error) {
      console.error('Error deleting user:', error);
      showError('Error', 'Failed to delete user');
    }
  };

  const handleAdminLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    if (loginLoading) return;

    setLoginLoading(true);

    try {
      const apiConfig = getApiConfig();
      const response = await fetch(`${apiConfig.baseUrl}/api/auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: loginForm.email,
          password: loginForm.password,
        }),
      });

      const data = await response.json();

      if (response.ok && data.success) {
        if (data.data.user.role !== 'Admin') {
          showError('Access denied', 'Only administrators can access this page');
          setLoginLoading(false);
          return;
        }

        // Store auth data
        localStorage.setItem('token', data.data.token);
        localStorage.setItem('user', JSON.stringify(data.data.user));

        // Set current user and load admin data
        setCurrentUser(data.data.user);
        setShowAdminLogin(false);
        setLoading(true);
        await loadData();
        
        showSuccess('Welcome back', `Hello ${data.data.user.firstName || 'Admin'}`);
      } else {
        showError('Login failed', data.message || 'Invalid credentials');
      }
    } catch (error) {
      console.error('Login error:', error);
      showError('Login failed', 'Unable to connect to server');
    } finally {
      setLoginLoading(false);
    }
  };

  const handleLogout = async () => {
    if (isLoggingOut) return;
    
    setIsLoggingOut(true);
    showToast('Logging out...', 'info');
    
    setTimeout(() => {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      localStorage.removeItem('showWelcome');
      setCurrentUser(null);
      setShowAdminLogin(true);
      setIsLoggingOut(false);
    }, 1000);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const formatLastSeen = (lastSeenAt?: string) => {
    if (!lastSeenAt) return 'Never';
    
    // Parse the UTC timestamp and create a proper Date object
    const lastSeen = new Date(lastSeenAt + (lastSeenAt.endsWith('Z') ? '' : 'Z'));
    const now = new Date();
    
    // Calculate difference based on local timezone dates
    const lastSeenLocalDate = new Date(lastSeen.getFullYear(), lastSeen.getMonth(), lastSeen.getDate());
    const nowLocalDate = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const diffMs = nowLocalDate.getTime() - lastSeenLocalDate.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) {
      // Format time for today in local timezone
      const timeString = lastSeen.toLocaleTimeString([], {
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
      });
      return `Today (${timeString})`;
    }
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays} days ago`;
    if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`;
    return `${Math.floor(diffDays / 30)} months ago`;
  };

  const formatSystemTime = (date: Date) => {
    return date.toLocaleString('en-US', {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      timeZoneName: 'short'
    });
  };

  // Show admin login form if not authenticated
  if (showAdminLogin) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center">
        <div className="max-w-md w-full mx-4">
          <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-8">
            <div className="text-center mb-8">
              <h1 className="text-2xl font-bold text-white mb-2">Admin Portal</h1>
              <p className="text-gray-400">Sign in to access the admin dashboard</p>
            </div>

            <form onSubmit={handleAdminLogin} className="space-y-6">
              <div>
                <label htmlFor="email" className="block text-sm font-medium text-gray-300 mb-2">
                  Email Address
                </label>
                <input
                  id="email"
                  type="email"
                  required
                  value={loginForm.email}
                  onChange={(e) => setLoginForm({ ...loginForm, email: e.target.value })}
                  className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded-md text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  placeholder="admin@example.com"
                  disabled={loginLoading}
                />
              </div>

              <div>
                <label htmlFor="password" className="block text-sm font-medium text-gray-300 mb-2">
                  Password
                </label>
                <input
                  id="password"
                  type="password"
                  required
                  value={loginForm.password}
                  onChange={(e) => setLoginForm({ ...loginForm, password: e.target.value })}
                  className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded-md text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  placeholder="Enter your password"
                  disabled={loginLoading}
                />
              </div>

              <button
                type="submit"
                disabled={loginLoading}
                className="w-full bg-red-600 text-white py-2 px-4 rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 focus:ring-offset-black transition-all duration-200 disabled:opacity-70 disabled:cursor-not-allowed flex items-center justify-center"
              >
                {loginLoading ? (
                  <>
                    <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Signing in...
                  </>
                ) : (
                  'Sign in to Admin Portal'
                )}
              </button>
            </form>

            <div className="mt-6 text-center">
              <p className="text-sm text-gray-400">
                Need user access? <a href="/login" className="text-blue-400 hover:text-blue-300">Sign in here</a>
              </p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center">
        <div className="text-white">Loading admin dashboard...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-black text-white">
      {/* Header */}
      <header className="bg-zinc-900 border-b border-zinc-800">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center space-x-4">
              <h1 className="text-xl font-bold text-white">Smart Expense Admin</h1>
              <span className="text-sm text-red-400 bg-red-900/20 px-2 py-1 rounded">Admin Panel</span>
            </div>

            <div className="flex items-center space-x-4">
              <div className="text-xs text-gray-400 font-mono bg-zinc-800 px-2 py-1 rounded">
                System: {formatSystemTime(currentTime)}
              </div>
              <Link 
                href="/dashboard"
                className="text-gray-300 hover:text-white transition-colors"
              >
                User Dashboard
              </Link>
              <span className="text-gray-300">
                {currentUser?.firstName || currentUser?.email}
              </span>
              <button
                onClick={handleLogout}
                disabled={isLoggingOut}
                className="bg-white text-black px-3 py-2 rounded text-sm hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-white focus:ring-offset-2 focus:ring-offset-black transition-all duration-200 disabled:opacity-70 disabled:cursor-not-allowed"
              >
                {isLoggingOut ? 'Logging out...' : 'Logout'}
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Stats Grid */}
        {stats && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
            <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
              <h3 className="text-sm font-medium text-gray-400">Total Users</h3>
              <p className="text-2xl font-bold text-white mt-1">{stats.totalUsers}</p>
            </div>
            <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
              <h3 className="text-sm font-medium text-gray-400">Admin Users</h3>
              <p className="text-2xl font-bold text-blue-400 mt-1">{stats.adminUsers}</p>
            </div>
            <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
              <h3 className="text-sm font-medium text-gray-400">Verified Users</h3>
              <p className="text-2xl font-bold text-green-400 mt-1">{stats.verifiedUsers}</p>
            </div>
            <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
              <h3 className="text-sm font-medium text-gray-400">Active Users (30d)</h3>
              <p className="text-2xl font-bold text-yellow-400 mt-1">{stats.activeUsers}</p>
            </div>
            <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
              <h3 className="text-sm font-medium text-gray-400">Total Transactions</h3>
              <p className="text-2xl font-bold text-purple-400 mt-1">{stats.totalTransactions}</p>
            </div>
            <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
              <h3 className="text-sm font-medium text-gray-400">Total Categories</h3>
              <p className="text-2xl font-bold text-orange-400 mt-1">{stats.totalCategories}</p>
            </div>
            <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
              <h3 className="text-sm font-medium text-gray-400">Total Budgets</h3>
              <p className="text-2xl font-bold text-pink-400 mt-1">{stats.totalBudgets}</p>
            </div>
            <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
              <h3 className="text-sm font-medium text-gray-400">New Users (7d)</h3>
              <p className="text-2xl font-bold text-cyan-400 mt-1">{stats.recentRegistrations}</p>
            </div>
          </div>
        )}

        {/* Users Table */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-lg">
          <div className="px-6 py-4 border-b border-zinc-800 flex justify-between items-center">
            <div>
              <h2 className="text-lg font-semibold text-white">User Management</h2>
              <p className="text-sm text-gray-400">Manage all registered users</p>
            </div>
            <div className="text-xs text-gray-500">
              <div>Last updated: {lastUpdated.toLocaleTimeString()}</div>
              <div className="text-green-400 mt-1">Auto-refresh: 60s</div>
            </div>
          </div>
          
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-zinc-800">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">User</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Role</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Status</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Activity</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Data</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Created</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-400 uppercase tracking-wider">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-zinc-800">
                {users.map((user) => (
                  <tr key={user.id} className="hover:bg-zinc-800/50">
                    <td className="px-6 py-4">
                      <div>
                        <div className="text-sm font-medium text-white">
                          {user.firstName && user.lastName ? `${user.firstName} ${user.lastName}` : 'No name'}
                        </div>
                        <div className="text-sm text-gray-400">{user.email}</div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                        user.role === 'Admin' 
                          ? 'bg-red-900/20 text-red-400' 
                          : 'bg-blue-900/20 text-blue-400'
                      }`}>
                        {user.role}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                        user.isEmailVerified 
                          ? 'bg-green-900/20 text-green-400' 
                          : 'bg-yellow-900/20 text-yellow-400'
                      }`}>
                        {user.isEmailVerified ? 'Verified' : 'Unverified'}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-300">
                      {formatLastSeen(user.lastSeenAt)}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-300">
                      <div>{user.transactionCount} transactions</div>
                      <div>{user.categoryCount} categories</div>
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-300">
                      {formatDate(user.createdAt)}
                    </td>
                    <td className="px-6 py-4 text-right">
                      {user.role !== 'Admin' && (
                        <button
                          onClick={() => deleteUser(user.id, user.email)}
                          className="text-red-400 hover:text-red-300 text-sm font-medium transition-colors"
                        >
                          Delete
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          
          {users.length === 0 && (
            <div className="px-6 py-12 text-center">
              <p className="text-gray-400">No users found</p>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}