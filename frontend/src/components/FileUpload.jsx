import React, { useRef, useState } from 'react';
import { Upload, X, CheckCircle, AlertCircle } from 'lucide-react';

const FileUpload = ({ onFileSelect, maxSize = 10 * 1024 * 1024, accept = '*' }) => {
  const fileInputRef = useRef(null);
  const [selectedFiles, setSelectedFiles] = useState([]);
  const [error, setError] = useState(null);

  const validateFile = (file) => {
    if (file.size > maxSize) {
      return `File size exceeds ${(maxSize / (1024 * 1024)).toFixed(2)}MB limit`;
    }
    return null;
  };

  const handleDragOver = (e) => {
    e.preventDefault();
    e.stopPropagation();
  };

  const handleDrop = (e) => {
    e.preventDefault();
    e.stopPropagation();

    const files = Array.from(e.dataTransfer.files);
    processFiles(files);
  };

  const handleFileInput = (e) => {
    const files = Array.from(e.target.files || []);
    processFiles(files);
  };

  const processFiles = (files) => {
    setError(null);
    const validatedFiles = [];

    files.forEach((file) => {
      const validationError = validateFile(file);
      if (validationError) {
        setError(validationError);
        return;
      }
      validatedFiles.push({
        id: `${file.name}-${Date.now()}`,
        file,
        name: file.name,
        size: file.size,
        status: 'pending',
      });
    });

    if (validatedFiles.length > 0) {
      setSelectedFiles((prev) => [...prev, ...validatedFiles]);
      onFileSelect?.(validatedFiles.map((f) => f.file));
    }
  };

  const removeFile = (id) => {
    setSelectedFiles((prev) => prev.filter((f) => f.id !== id));
  };

  const formatFileSize = (bytes) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  };

  return (
    <div className="w-full">
      {/* Upload Area */}
      <div
        onDragOver={handleDragOver}
        onDrop={handleDrop}
        className="relative border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg p-8 text-center cursor-pointer transition-colors hover:border-primary-500 hover:bg-primary-50/30 dark:hover:bg-primary-900/10"
        onClick={() => fileInputRef.current?.click()}
      >
        <input
          ref={fileInputRef}
          type="file"
          multiple
          accept={accept}
          onChange={handleFileInput}
          className="hidden"
          aria-label="File upload"
        />

        <div className="flex flex-col items-center justify-center gap-3">
          <div className="p-3 bg-primary-100 dark:bg-primary-900/30 rounded-lg">
            <Upload className="w-6 h-6 text-primary-600 dark:text-primary-400" />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-900 dark:text-white">
              Drag and drop your files here, or click to browse
            </p>
            <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
              Supported: Configuration files, photos, logs
            </p>
            <p className="text-xs text-gray-500 dark:text-gray-500 mt-1">
              Max size: {(maxSize / (1024 * 1024)).toFixed(2)}MB
            </p>
          </div>
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mt-3 flex items-center gap-2 p-3 bg-danger-50 dark:bg-danger-900/20 border border-danger-300 dark:border-danger-700 rounded-lg text-danger-700 dark:text-danger-200">
          <AlertCircle size={18} className="flex-shrink-0" />
          <span className="text-sm">{error}</span>
        </div>
      )}

      {/* Selected Files List */}
      {selectedFiles.length > 0 && (
        <div className="mt-4 space-y-2">
          <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
            Selected Files ({selectedFiles.length})
          </p>
          <div className="space-y-2">
            {selectedFiles.map((item) => (
              <div
                key={item.id}
                className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg"
              >
                <div className="flex items-center gap-3 flex-1">
                  <div className="flex-shrink-0">
                    {item.status === 'pending' && (
                      <div className="w-5 h-5 border-2 border-gray-300 dark:border-gray-600 border-t-primary-600 rounded-full animate-spin"></div>
                    )}
                    {item.status === 'completed' && (
                      <CheckCircle size={20} className="text-success-500" />
                    )}
                    {item.status === 'error' && (
                      <AlertCircle size={20} className="text-danger-500" />
                    )}
                  </div>
                  <div className="flex-1">
                    <p className="text-sm font-medium text-gray-900 dark:text-white truncate">
                      {item.name}
                    </p>
                    <p className="text-xs text-gray-600 dark:text-gray-400">
                      {formatFileSize(item.size)}
                    </p>
                  </div>
                </div>
                <button
                  onClick={() => removeFile(item.id)}
                  className="flex-shrink-0 p-1 hover:bg-gray-200 dark:hover:bg-gray-700 rounded transition-colors text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white"
                  aria-label={`Remove ${item.name}`}
                >
                  <X size={18} />
                </button>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export default FileUpload;
