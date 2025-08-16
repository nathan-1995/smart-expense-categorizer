import Link from "next/link";

export default function Home() {
  return (
    <div className="min-h-screen bg-white dark:bg-black text-black dark:text-white">
      {/* Navigation */}
      <nav className="border-b border-gray-200 dark:border-gray-800">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="font-bold text-xl">
              Smart Expense
            </div>
            <div className="flex space-x-4">
              <Link 
                href="/login"
                className="text-gray-600 dark:text-gray-300 hover:text-black dark:hover:text-white transition-colors"
              >
                Sign in
              </Link>
              <Link 
                href="/register"
                className="bg-black dark:bg-white text-white dark:text-black px-4 py-2 rounded-md font-medium hover:bg-gray-800 dark:hover:bg-gray-200 transition-colors"
              >
                Get started
              </Link>
            </div>
          </div>
        </div>
      </nav>

      {/* Hero Section */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
        <div className="text-center max-w-4xl mx-auto">
          <h1 className="text-5xl sm:text-6xl lg:text-7xl font-bold tracking-tight mb-8">
            Categorize your{" "}
            <span className="bg-gradient-to-r from-black to-gray-600 dark:from-white dark:to-gray-400 bg-clip-text text-transparent">
              expenses
            </span>{" "}
            automatically
          </h1>
          
          <p className="text-xl sm:text-2xl text-gray-600 dark:text-gray-400 mb-12 leading-relaxed">
            Stop manually sorting receipts. Let AI understand your spending patterns and categorize transactions instantly with machine learning.
          </p>

          <div className="flex flex-col sm:flex-row gap-4 justify-center mb-16">
            <Link 
              href="/register"
              className="bg-black dark:bg-white text-white dark:text-black px-8 py-4 rounded-lg font-medium text-lg hover:bg-gray-800 dark:hover:bg-gray-200 transition-colors"
            >
              Start organizing →
            </Link>
            <Link 
              href="/login"
              className="border border-gray-300 dark:border-gray-700 px-8 py-4 rounded-lg font-medium text-lg hover:border-gray-400 dark:hover:border-gray-600 transition-colors"
            >
              Sign in
            </Link>
          </div>
        </div>

        {/* Features Grid */}
        <div className="grid md:grid-cols-3 gap-8 mt-20">
          <div className="text-center">
            <div className="w-12 h-12 bg-black dark:bg-white rounded-lg mx-auto mb-4 flex items-center justify-center">
              <svg className="w-6 h-6 text-white dark:text-black" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </div>
            <h3 className="text-xl font-semibold mb-2">AI-Powered Categorization</h3>
            <p className="text-gray-600 dark:text-gray-400">
              Advanced machine learning algorithms automatically categorize your transactions with high accuracy.
            </p>
          </div>

          <div className="text-center">
            <div className="w-12 h-12 bg-black dark:bg-white rounded-lg mx-auto mb-4 flex items-center justify-center">
              <svg className="w-6 h-6 text-white dark:text-black" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
            </div>
            <h3 className="text-xl font-semibold mb-2">Smart Budgeting</h3>
            <p className="text-gray-600 dark:text-gray-400">
              Set monthly limits per category and get alerts when you're approaching your budget threshold.
            </p>
          </div>

          <div className="text-center">
            <div className="w-12 h-12 bg-black dark:bg-white rounded-lg mx-auto mb-4 flex items-center justify-center">
              <svg className="w-6 h-6 text-white dark:text-black" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M9 19l3 3m0 0l3-3m-3 3V10" />
              </svg>
            </div>
            <h3 className="text-xl font-semibold mb-2">Multiple Import Options</h3>
            <p className="text-gray-600 dark:text-gray-400">
              Import transactions via CSV files, scan receipts with OCR, or add them manually.
            </p>
          </div>
        </div>

        {/* Call to Action */}
        <div className="text-center mt-20 py-16 border-t border-gray-200 dark:border-gray-800">
          <h2 className="text-3xl font-bold mb-4">
            Ready to take control of your expenses?
          </h2>
          <p className="text-xl text-gray-600 dark:text-gray-400 mb-8">
            Join thousands of users who have organized their financial life.
          </p>
          <Link 
            href="/register"
            className="bg-black dark:bg-white text-white dark:text-black px-8 py-4 rounded-lg font-medium text-lg hover:bg-gray-800 dark:hover:bg-gray-200 transition-colors inline-block"
          >
            Get started for free
          </Link>
        </div>
      </main>

      {/* Footer */}
      <footer className="border-t border-gray-200 dark:border-gray-800 py-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <p className="text-gray-600 dark:text-gray-400">
            © 2024 Smart Expense Categorizer. Built with Next.js and AI.
          </p>
        </div>
      </footer>
    </div>
  );
}
