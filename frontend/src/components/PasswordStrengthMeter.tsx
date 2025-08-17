'use client';

import { useMemo } from 'react';

interface PasswordStrengthMeterProps {
  password: string;
  showRequirements?: boolean;
}

interface PasswordRequirement {
  id: string;
  label: string;
  test: (password: string) => boolean;
}

const passwordRequirements: PasswordRequirement[] = [
  {
    id: 'length',
    label: 'At least 8 characters',
    test: (password) => password.length >= 8,
  },
  {
    id: 'uppercase',
    label: 'One uppercase letter',
    test: (password) => /[A-Z]/.test(password),
  },
  {
    id: 'lowercase',
    label: 'One lowercase letter',
    test: (password) => /[a-z]/.test(password),
  },
  {
    id: 'number',
    label: 'One number',
    test: (password) => /\d/.test(password),
  },
  {
    id: 'special',
    label: 'One special character (!@#$%^&*)',
    test: (password) => /[!@#$%^&*(),.?":{}|<>]/.test(password),
  },
];

const PasswordStrengthMeter = ({ password, showRequirements = true }: PasswordStrengthMeterProps) => {
  const analysis = useMemo(() => {
    const metRequirements = passwordRequirements.filter(req => req.test(password));
    const score = metRequirements.length;
    
    let strength: 'weak' | 'medium' | 'strong' = 'weak';
    let color = 'bg-red-500';
    let label = 'Weak';
    
    if (score >= 5) {
      strength = 'strong';
      color = 'bg-green-500';
      label = 'Strong';
    } else if (score >= 3) {
      strength = 'medium';
      color = 'bg-yellow-500';
      label = 'Medium';
    }
    
    const percentage = (score / passwordRequirements.length) * 100;
    
    return {
      score,
      strength,
      color,
      label,
      percentage,
      metRequirements,
    };
  }, [password]);

  if (!password) {
    return null;
  }

  return (
    <div className="mt-2 space-y-2">
      {/* Strength Meter */}
      <div className="space-y-1">
        <div className="flex justify-between text-xs">
          <span className="text-gray-400">Password strength</span>
          <span className={`font-medium ${
            analysis.strength === 'strong' ? 'text-green-400' :
            analysis.strength === 'medium' ? 'text-yellow-400' :
            'text-red-400'
          }`}>
            {analysis.label}
          </span>
        </div>
        <div className="h-2 bg-gray-700 rounded-full overflow-hidden">
          <div
            className={`h-full transition-all duration-300 ${analysis.color}`}
            style={{ width: `${analysis.percentage}%` }}
          />
        </div>
      </div>

      {/* Requirements List */}
      {showRequirements && (
        <div className="space-y-1">
          <span className="text-xs text-gray-400">Password must contain:</span>
          <ul className="space-y-1">
            {passwordRequirements.map((requirement) => {
              const isMet = analysis.metRequirements.includes(requirement);
              return (
                <li
                  key={requirement.id}
                  className={`flex items-center text-xs transition-colors duration-200 ${
                    isMet ? 'text-green-400' : 'text-gray-500'
                  }`}
                >
                  <div className={`w-3 h-3 rounded-full mr-2 flex items-center justify-center transition-colors duration-200 ${
                    isMet ? 'bg-green-500' : 'bg-gray-600'
                  }`}>
                    {isMet && (
                      <svg className="w-2 h-2 text-white" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                      </svg>
                    )}
                  </div>
                  {requirement.label}
                </li>
              );
            })}
          </ul>
        </div>
      )}
    </div>
  );
};

export default PasswordStrengthMeter;
export { passwordRequirements };