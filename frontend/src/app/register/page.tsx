'use client';

import { useState } from 'react';
import { useToast } from '../../components/ToastProvider';
import { getAuthUrl, getHeaders } from '../../lib/api';
import PasswordStrengthMeter from '../../components/PasswordStrengthMeter';

interface RegisterResponse {
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

interface FormErrors {
  email?: string;
  password?: string;
  confirmPassword?: string;
  firstName?: string;
  lastName?: string;
  terms?: string;
}

export default function RegisterPage() {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    agreeToTerms: false,
  });
  
  const [isLoading, setIsLoading] = useState(false);
  const [isRegistered, setIsRegistered] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({});
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const { showSuccess, showError } = useToast();

  // Real-time validation
  const validateField = (name: string, value: string | boolean) => {
    const newErrors = { ...errors };

    switch (name) {
      case 'email':
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!value) {
          newErrors.email = 'Email is required';
        } else if (!emailRegex.test(value as string)) {
          newErrors.email = 'Please enter a valid email address';
        } else {
          delete newErrors.email;
        }
        break;

      case 'password':
        if (!value) {
          newErrors.password = 'Password is required';
        } else if ((value as string).length < 8) {
          newErrors.password = 'Password must be at least 8 characters';
        } else {
          delete newErrors.password;
        }
        break;

      case 'confirmPassword':
        if (!value) {
          newErrors.confirmPassword = 'Please confirm your password';
        } else if (value !== formData.password) {
          newErrors.confirmPassword = 'Passwords do not match';
        } else {
          delete newErrors.confirmPassword;
        }
        break;

      case 'firstName':
        if (!value) {
          newErrors.firstName = 'First name is required';
        } else if ((value as string).length < 2) {
          newErrors.firstName = 'First name must be at least 2 characters';
        } else {
          delete newErrors.firstName;
        }
        break;

      case 'lastName':
        if (!value) {
          newErrors.lastName = 'Last name is required';
        } else if ((value as string).length < 2) {
          newErrors.lastName = 'Last name must be at least 2 characters';
        } else {
          delete newErrors.lastName;
        }
        break;

      case 'agreeToTerms':
        if (!value) {
          newErrors.terms = 'You must agree to the terms and conditions';
        } else {
          delete newErrors.terms;
        }
        break;
    }

    setErrors(newErrors);
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    const fieldValue = type === 'checkbox' ? checked : value;
    
    setFormData(prev => ({
      ...prev,
      [name]: fieldValue,
    }));

    // Real-time validation
    validateField(name, fieldValue);
    
    // Also validate confirm password when password changes
    if (name === 'password' && formData.confirmPassword) {
      validateField('confirmPassword', formData.confirmPassword);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    // Final validation
    Object.entries(formData).forEach(([key, value]) => {
      validateField(key, value);
    });

    if (Object.keys(errors).length > 0) {
      setIsLoading(false);
      showError('Please fix the errors below', 'Check all required fields');
      return;
    }

    try {
      const response = await fetch(getAuthUrl('/register'), {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({
          email: formData.email,
          password: formData.password,
          confirmPassword: formData.confirmPassword,
          firstName: formData.firstName,
          lastName: formData.lastName,
        }),
      });

      const data: RegisterResponse = await response.json();

      if (data.success && data.data) {
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
        
        // Set registration success state
        setIsRegistered(true);
        
        // Show success message
        showSuccess('Account created successfully!', `Welcome to Smart Expense, ${userName}!`);
        
        // Smooth redirect with proper delay to show toast
        setTimeout(() => {
          window.location.href = '/dashboard';
        }, 2000);
      } else {
        showError('Registration failed', data.message || 'Please check your information and try again.');
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Network error - please try again';
      showError('Connection error', errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  const isFormValid = 
    formData.email &&
    formData.password &&
    formData.confirmPassword &&
    formData.firstName &&
    formData.lastName &&
    formData.agreeToTerms &&
    Object.keys(errors).length === 0;

  return (
    <div className="min-h-screen bg-black flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        {/* Logo/Brand */}
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-white mb-2">
            Smart Expense
          </h1>
          <p className="text-gray-400 text-sm">
            Start managing your expenses intelligently
          </p>
        </div>

        {/* Registration Form */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-8">
          <h2 className="text-xl font-semibold text-white mb-6 text-center">
            Create your account
          </h2>
          
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Name Fields */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="firstName" className="block text-sm font-medium text-gray-300 mb-2">
                  First Name
                </label>
                <input
                  id="firstName"
                  name="firstName"
                  type="text"
                  autoComplete="given-name"
                  value={formData.firstName}
                  onChange={handleChange}
                  required
                  className={`w-full px-3 py-2 bg-black border rounded-md text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-white focus:border-transparent transition-all duration-200 ${
                    errors.firstName ? 'border-red-500' : 'border-zinc-700'
                  }`}
                  placeholder="John"
                />
                {errors.firstName && (
                  <p className="mt-1 text-sm text-red-400">{errors.firstName}</p>
                )}
              </div>

              <div>
                <label htmlFor="lastName" className="block text-sm font-medium text-gray-300 mb-2">
                  Last Name
                </label>
                <input
                  id="lastName"
                  name="lastName"
                  type="text"
                  autoComplete="family-name"
                  value={formData.lastName}
                  onChange={handleChange}
                  required
                  className={`w-full px-3 py-2 bg-black border rounded-md text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-white focus:border-transparent transition-all duration-200 ${
                    errors.lastName ? 'border-red-500' : 'border-zinc-700'
                  }`}
                  placeholder="Doe"
                />
                {errors.lastName && (
                  <p className="mt-1 text-sm text-red-400">{errors.lastName}</p>
                )}
              </div>
            </div>

            {/* Email Field */}
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-300 mb-2">
                Email Address
              </label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                value={formData.email}
                onChange={handleChange}
                required
                className={`w-full px-3 py-2 bg-black border rounded-md text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-white focus:border-transparent transition-all duration-200 ${
                  errors.email ? 'border-red-500' : 'border-zinc-700'
                }`}
                placeholder="john@example.com"
              />
              {errors.email && (
                <p className="mt-1 text-sm text-red-400">{errors.email}</p>
              )}
            </div>

            {/* Password Field */}
            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-300 mb-2">
                Password
              </label>
              <div className="relative">
                <input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  value={formData.password}
                  onChange={handleChange}
                  required
                  className={`w-full px-3 py-2 pr-10 bg-black border rounded-md text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-white focus:border-transparent transition-all duration-200 ${
                    errors.password ? 'border-red-500' : 'border-zinc-700'
                  }`}
                  placeholder="Create a strong password"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-white transition-colors"
                >
                  {showPassword ? (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L3 3m6.878 6.878L21 21" />
                    </svg>
                  ) : (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                  )}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1 text-sm text-red-400">{errors.password}</p>
              )}
              <PasswordStrengthMeter password={formData.password} />
            </div>

            {/* Confirm Password Field */}
            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-300 mb-2">
                Confirm Password
              </label>
              <div className="relative">
                <input
                  id="confirmPassword"
                  name="confirmPassword"
                  type={showConfirmPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  value={formData.confirmPassword}
                  onChange={handleChange}
                  required
                  className={`w-full px-3 py-2 pr-10 bg-black border rounded-md text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-white focus:border-transparent transition-all duration-200 ${
                    errors.confirmPassword ? 'border-red-500' : 'border-zinc-700'
                  }`}
                  placeholder="Confirm your password"
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-white transition-colors"
                >
                  {showConfirmPassword ? (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L3 3m6.878 6.878L21 21" />
                    </svg>
                  ) : (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                  )}
                </button>
              </div>
              {errors.confirmPassword && (
                <p className="mt-1 text-sm text-red-400">{errors.confirmPassword}</p>
              )}
              {formData.confirmPassword && !errors.confirmPassword && formData.password === formData.confirmPassword && (
                <p className="mt-1 text-sm text-green-400 flex items-center">
                  <svg className="w-3 h-3 mr-1" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                  Passwords match
                </p>
              )}
            </div>

            {/* Terms and Conditions */}
            <div>
              <label className="flex items-start space-x-3 cursor-pointer">
                <input
                  type="checkbox"
                  name="agreeToTerms"
                  checked={formData.agreeToTerms}
                  onChange={handleChange}
                  className="mt-1 w-4 h-4 text-white bg-black border-zinc-700 rounded focus:ring-white focus:ring-2"
                />
                <span className={`text-sm ${errors.terms ? 'text-red-400' : 'text-gray-300'}`}>
                  I agree to the{' '}
                  <a href="#" className="text-white hover:underline">
                    Terms of Service
                  </a>{' '}
                  and{' '}
                  <a href="#" className="text-white hover:underline">
                    Privacy Policy
                  </a>
                </span>
              </label>
              {errors.terms && (
                <p className="mt-1 text-sm text-red-400">{errors.terms}</p>
              )}
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              disabled={isLoading || !isFormValid || isRegistered}
              className={`w-full font-medium py-3 px-4 rounded-md focus:outline-none focus:ring-2 focus:ring-white focus:ring-offset-2 focus:ring-offset-black transition-all duration-200 disabled:cursor-not-allowed ${
                isRegistered 
                  ? 'bg-green-600 text-white disabled:hover:bg-green-600' 
                  : 'bg-white text-black hover:bg-gray-100 disabled:opacity-50 disabled:hover:bg-white'
              }`}
            >
              <div className="flex items-center justify-center">
                {(isLoading || isRegistered) && (
                  <svg className={`animate-spin -ml-1 mr-3 h-4 w-4 ${isRegistered ? 'text-white' : 'text-black'}`} xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                )}
                {isRegistered && (
                  <svg className="w-4 h-4 text-white mr-2" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                )}
                <span className={`transition-all duration-200 ${(isLoading || isRegistered) ? 'opacity-75' : ''}`}>
                  {isRegistered ? 'Redirecting to dashboard...' : isLoading ? 'Creating account...' : 'Create Account'}
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
          <div className="mt-6 text-center">
            <p className="text-sm text-gray-500">
              Already have an account?{' '}
              <a href="/login" className="text-white hover:underline">
                Sign in
              </a>
            </p>
          </div>
        </div>

        {/* Terms */}
        <p className="mt-8 text-center text-xs text-gray-500">
          By creating an account, you agree to our{' '}
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