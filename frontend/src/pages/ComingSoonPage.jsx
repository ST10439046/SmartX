import React from 'react';
import { Lock } from 'lucide-react';

const ComingSoonPage = ({ title, description }) => {
  return (
    <div className="flex flex-col items-center justify-center min-h-screen">
      <div className="text-center space-y-6">
        <div className="inline-flex items-center justify-center w-20 h-20 bg-primary-100 dark:bg-primary-900/30 rounded-full">
          <Lock size={40} className="text-primary-600 dark:text-primary-400" />
        </div>
        <div>
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-2">
            {title}
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400 max-w-md mx-auto">
            {description}
          </p>
        </div>
        <div className="inline-block px-6 py-3 bg-primary-600 text-white font-medium rounded-lg">
          Coming Soon
        </div>
        <p className="text-sm text-gray-500 dark:text-gray-400 pt-4">
          This feature is currently under development. Check back soon!
        </p>
      </div>
    </div>
  );
};

export default ComingSoonPage;
