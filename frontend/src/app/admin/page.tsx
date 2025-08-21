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
  const { showSuccess, showError, showToast } = useToast();

  useEffect(() => {
    // Check if user is logged in and is admin
    const token = localStorage.getItem('token');
    const userData = localStorage.getItem('user');

    if (!token || !userData) {
      window.location.href = '/login';
      return;
    }

    try {
      const user = JSON.parse(userData);
      if (user.role !== 'Admin') {
        showError('Access denied', 'Admin privileges required');
        window.location.href = '/dashboard';
        return;
      }
      setCurrentUser(user);
      loadData();
    } catch (error) {
      console.error('Error parsing user data:', error);
      window.location.href = '/login';
    }
  }, []);

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

  const handleLogout = async () => {
    if (isLoggingOut) return;
    
    setIsLoggingOut(true);
    showToast('Logging out...', 'info');
    
    setTimeout(() => {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      localStorage.removeItem('showWelcome');
      window.location.href = '/';
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
    
    const lastSeen = new Date(lastSeenAt);
    const now = new Date();
    const diffMs = now.getTime() - lastSeen.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays} days ago`;
    if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`;
    return `${Math.floor(diffDays / 30)} months ago`;
  };

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
          <div className="px-6 py-4 border-b border-zinc-800">
            <h2 className="text-lg font-semibold text-white">User Management</h2>
            <p className="text-sm text-gray-400">Manage all registered users</p>
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