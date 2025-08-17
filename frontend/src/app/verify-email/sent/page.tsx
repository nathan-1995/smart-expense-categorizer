'use client'

import { useState } from 'react'
import Link from 'next/link'
import { useToast } from '@/components/ToastProvider'
import { getApiConfig } from '@/lib/api'

export default function VerificationSentPage() {
  const [isResending, setIsResending] = useState(false)
  const [email, setEmail] = useState('')
  const { showToast } = useToast()
  
  // Get email from localStorage if available (from registration flow)
  useState(() => {
    if (typeof window !== 'undefined') {
      const storedEmail = localStorage.getItem('pendingVerificationEmail')
      if (storedEmail) {
        setEmail(storedEmail)
      }
    }
  })
  
  const resendVerification = async () => {
    if (!email.trim()) {
      showToast('Please enter your email address', 'error')
      return
    }
    
    setIsResending(true)
    
    try {
      const apiConfig = getApiConfig()
      const response = await fetch(`${apiConfig.baseUrl}/api/auth/resend-verification`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email: email.trim() }),
      })
      
      const data = await response.json()
      showToast(data.message || 'Verification email sent', data.success ? 'success' : 'info')
    } catch (error) {
      showToast('Failed to resend verification email', 'error')
    } finally {
      setIsResending(false)
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
          <div className="text-center">
            <div className="w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 4.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              </svg>
            </div>
            
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Check your email! 📧</h1>
            <p className="text-gray-600 mb-6">
              We've sent a verification link to your email address. Please check your inbox and click the link to verify your account.
            </p>
            
            {email && (
              <div className="bg-gray-50 rounded-lg p-4 mb-6">
                <p className="text-sm text-gray-700">
                  Verification email sent to:
                </p>
                <p className="font-medium text-gray-900">{email}</p>
              </div>
            )}
            
            <div className="space-y-4">
              <p className="text-sm text-gray-600">
                Can't find the email? Check your spam folder or request a new one.
              </p>
              
              <div className="space-y-3">
                {!email && (
                  <div>
                    <input
                      type="email"
                      placeholder="Enter your email address"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      className="block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    />
                  </div>
                )}
                
                <button
                  onClick={resendVerification}
                  disabled={isResending || !email}
                  className="w-full flex justify-center items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {isResending ? (
                    <>
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                      Sending...
                    </>
                  ) : (
                    'Resend verification email'
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
        
        <div className="mt-6 text-center space-y-2">
          <Link 
            href="/login"
            className="block text-blue-600 hover:text-blue-500 transition-colors"
          >
            Back to login
          </Link>
          
          <p className="text-xs text-gray-500">
            Having trouble? <a href="mailto:support@smartexpense.com" className="text-blue-600 hover:text-blue-500">Contact support</a>
          </p>
        </div>
      </div>
      
      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-white border border-gray-200 rounded-lg p-4">
          <h3 className="text-sm font-medium text-gray-900 mb-2">What to expect:</h3>
          <ul className="text-xs text-gray-600 space-y-1">
            <li>• Verification emails usually arrive within 1-2 minutes</li>
            <li>• The verification link expires after 24 hours</li>
            <li>• Once verified, you'll be redirected to your dashboard</li>
            <li>• You'll receive a welcome email after successful verification</li>
          </ul>
        </div>
      </div>
    </div>
  )
}