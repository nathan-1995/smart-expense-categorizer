'use client'

import { useEffect, useState } from 'react'
import { useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { useToast } from '@/components/ToastProvider'
import { getApiConfig } from '@/lib/api'

type VerificationState = 'verifying' | 'success' | 'error' | 'expired' | 'invalid'

export default function VerifyEmailPage() {
  const [state, setState] = useState<VerificationState>('verifying')
  const [email, setEmail] = useState<string>('')
  const searchParams = useSearchParams()
  const { showToast } = useToast()
  
  useEffect(() => {
    const token = searchParams.get('token')
    
    if (!token) {
      setState('invalid')
      return
    }
    
    verifyEmail(token)
  }, [searchParams])
  
  const verifyEmail = async (token: string) => {
    try {
      const apiConfig = getApiConfig()
      const response = await fetch(`${apiConfig.baseUrl}/api/auth/verify-email`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ token }),
      })
      
      const data = await response.json()
      
      if (data.success) {
        setState('success')
        setEmail(data.data?.email || '')
        
        // Update user data in localStorage to reflect verification
        const userData = localStorage.getItem('user')
        if (userData) {
          try {
            const user = JSON.parse(userData)
            user.isEmailVerified = true
            localStorage.setItem('user', JSON.stringify(user))
          } catch (error) {
            console.error('Error updating user data:', error)
          }
        }
        
        showToast('Email verified successfully! Welcome to Smart Expense!', 'success')
      } else {
        // Check error message for specific cases
        if (data.message?.includes('expired')) {
          setState('expired')
        } else {
          setState('error')
        }
        showToast(data.message || 'Email verification failed', 'error')
      }
    } catch (error) {
      console.error('Verification error:', error)
      setState('error')
      showToast('Network error during verification', 'error')
    }
  }
  
  const resendVerification = async () => {
    if (!email) return
    
    try {
      const apiConfig = getApiConfig()
      const response = await fetch(`${apiConfig.baseUrl}/api/auth/resend-verification`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email }),
      })
      
      const data = await response.json()
      showToast(data.message || 'Verification email sent', data.success ? 'success' : 'info')
    } catch (error) {
      showToast('Failed to resend verification email', 'error')
    }
  }
  
  const renderContent = () => {
    switch (state) {
      case 'verifying':
        return (
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Verifying your email...</h1>
            <p className="text-gray-600">Please wait while we verify your email address.</p>
          </div>
        )
      
      case 'success':
        return (
          <div className="text-center">
            <div className="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Email verified successfully! 🎉</h1>
            <p className="text-gray-600 mb-6">
              Your email has been verified and your account is now active.
              {email && ` Welcome to Smart Expense!`}
            </p>
            <Link 
              href="/dashboard"
              className="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
            >
              Go to Dashboard
              <svg className="ml-2 -mr-1 w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
            </Link>
          </div>
        )
      
      case 'expired':
        return (
          <div className="text-center">
            <div className="w-12 h-12 bg-yellow-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-6 h-6 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16c-.77.833.192 2.5 1.732 2.5z" />
              </svg>
            </div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Verification link expired</h1>
            <p className="text-gray-600 mb-6">
              Your verification link has expired. Please request a new verification email.
            </p>
            <div className="space-y-4">
              {email && (
                <button
                  onClick={resendVerification}
                  className="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
                >
                  Resend verification email
                </button>
              )}
              <div>
                <Link 
                  href="/login"
                  className="text-blue-600 hover:text-blue-500 transition-colors"
                >
                  Back to login
                </Link>
              </div>
            </div>
          </div>
        )
      
      case 'error':
        return (
          <div className="text-center">
            <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-6 h-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Verification failed</h1>
            <p className="text-gray-600 mb-6">
              We couldn't verify your email address. The link may be invalid or already used.
            </p>
            <div className="space-y-4">
              <Link 
                href="/register"
                className="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
              >
                Create new account
              </Link>
              <div>
                <Link 
                  href="/login"
                  className="text-blue-600 hover:text-blue-500 transition-colors"
                >
                  Back to login
                </Link>
              </div>
            </div>
          </div>
        )
      
      case 'invalid':
        return (
          <div className="text-center">
            <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-6 h-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16c-.77.833.192 2.5 1.732 2.5z" />
              </svg>
            </div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Invalid verification link</h1>
            <p className="text-gray-600 mb-6">
              The verification link is invalid or missing. Please check your email or request a new verification link.
            </p>
            <Link 
              href="/register"
              className="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
            >
              Create new account
            </Link>
          </div>
        )
    }
  }
  
  return (
    <div className="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div className="sm:mx-auto sm:w-full sm:max-w-md">
        <h2 className="text-center text-3xl font-extrabold text-gray-900 mb-2">
          Smart Expense
        </h2>
        <p className="text-center text-sm text-gray-600">
          AI-powered expense categorization
        </p>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
          {renderContent()}
        </div>
        
        <div className="mt-6 text-center">
          <p className="text-xs text-gray-500">
            Having trouble? <a href="mailto:support@smartexpense.com" className="text-blue-600 hover:text-blue-500">Contact support</a>
          </p>
        </div>
      </div>
    </div>
  )
}