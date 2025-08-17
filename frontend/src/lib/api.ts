// API configuration utility

class ApiConfig {
  private static instance: ApiConfig;
  public readonly baseUrl: string;
  public readonly environment: string;

  private constructor() {
    // Get API URL from environment variables
    this.baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    this.environment = process.env.NEXT_PUBLIC_ENV || 'development';

    // Validate that API URL is set
    if (!process.env.NEXT_PUBLIC_API_URL && this.environment === 'production') {
      console.warn('NEXT_PUBLIC_API_URL not set in production environment');
    }
  }

  public static getInstance(): ApiConfig {
    if (!ApiConfig.instance) {
      ApiConfig.instance = new ApiConfig();
    }
    return ApiConfig.instance;
  }

  // API endpoint builders
  public getAuthUrl(endpoint: string): string {
    return `${this.baseUrl}/api/auth${endpoint}`;
  }

  public getApiUrl(endpoint: string): string {
    return `${this.baseUrl}/api${endpoint}`;
  }

  // Common headers for API requests
  public getHeaders(includeAuth = false): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (includeAuth) {
      const token = typeof window !== 'undefined' ? localStorage.getItem('token') : null;
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }
    }

    return headers;
  }

  // Development helper
  public logConfig(): void {
    if (this.environment === 'development') {
      console.log('API Configuration:', {
        baseUrl: this.baseUrl,
        environment: this.environment,
      });
    }
  }
}

// Export singleton instance
export const apiConfig = ApiConfig.getInstance();

// Export convenience functions
export const getAuthUrl = (endpoint: string) => apiConfig.getAuthUrl(endpoint);
export const getApiUrl = (endpoint: string) => apiConfig.getApiUrl(endpoint);
export const getHeaders = (includeAuth = false) => apiConfig.getHeaders(includeAuth);