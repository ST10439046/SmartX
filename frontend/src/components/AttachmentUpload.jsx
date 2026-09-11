import { useState } from 'react';
import { Upload, X, Loader, CheckCircle, AlertCircle } from 'lucide-react';
import { attachmentApi } from '../services/apiClient';
import { useNotification } from '../hooks';

const getFileType = (filename) => {
  const ext = filename.split('.').pop().toLowerCase();
  if (['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp'].includes(ext)) return 'photo';
  if (['json', 'xml', 'csv', 'conf', 'yaml', 'yml', 'txt'].includes(ext)) return 'config';
  if (['log'].includes(ext)) return 'log';
  if (['pdf', 'doc', 'docx', 'xls', 'xlsx'].includes(ext)) return 'document';
  return 'other';
};

const AttachmentUpload = ({ sensorMac, onUploadSuccess }) => {
    const [files, setFiles] = useState([]);
    const [uploading, setUploading] = useState(false);
    const [dragActive, setDragActive] = useState(false);

    const { show } = useNotification();

    const allowedExtensions = ['.csv', '.json', '.txt', '.xml', '.log', '.pdf', '.xlsx', '.xls', '.jpg', '.jpeg', '.png', '.gif', '.bmp'];

    const handleDrag = (e) => {
        e.preventDefault();
        e.stopPropagation();
        if (e.type === 'dragenter' || e.type === 'dragover') {
            setDragActive(true);
        } else if (e.type === 'dragleave') {
            setDragActive(false);
        }
    };

    const validateFile = (selectedFile) => {
        const fileExtension = selectedFile.name.substring(selectedFile.name.lastIndexOf('.')).toLowerCase();

        if (!allowedExtensions.includes(fileExtension)) {
            show(
                `File type ${fileExtension} is not allowed. Allowed types: ${allowedExtensions.join(', ')}`,
                'error'
            );
            return false;
        }

        const maxSize = 50 * 1024 * 1024; // 50 MB
        if (selectedFile.size > maxSize) {
            show('File size exceeds 50 MB limit', 'error');
            return false;
        }

        return true;
    };

    const handleDrop = (e) => {
        e.preventDefault();
        e.stopPropagation();
        setDragActive(false);

        const droppedFiles = e.dataTransfer.files;
        if (droppedFiles.length > 0) {
            processFiles(Array.from(droppedFiles));
        }
    };

    const handleFileInput = (e) => {
        const selectedFiles = e.target.files;
        if (selectedFiles && selectedFiles.length > 0) {
            processFiles(Array.from(selectedFiles));
        }
    };

    const processFiles = (filesToAdd) => {
        const validatedFiles = [];
        filesToAdd.forEach((file) => {
            if (validateFile(file)) {
                validatedFiles.push({
                    id: `${file.name}-${Date.now()}-${Math.random()}`,
                    file,
                    name: file.name,
                    size: file.size,
                    status: 'pending',
                });
            }
        });

        if (validatedFiles.length > 0) {
            setFiles((prev) => [...prev, ...validatedFiles]);
        }
    };

    const removeFile = (id) => {
        setFiles((prev) => prev.filter((f) => f.id !== id));
    };

    const formatFileSize = (bytes) => {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
    };

    const uploadFiles = async () => {
        if (files.length === 0) {
            show('Please select files to upload', 'warning');
            return;
        }

        if (!sensorMac) {
            show('Sensor MAC address not available', 'error');
            console.error('sensorMac is undefined or empty:', sensorMac);
            return;
        }

        setUploading(true);
        let successCount = 0;
        let failedCount = 0;

        try {
            for (const fileObj of files) {
                try {
                    // Update status to uploading
                    setFiles((prev) =>
                        prev.map((f) =>
                            f.id === fileObj.id ? { ...f, status: 'uploading' } : f
                        )
                    );

                    console.log('Uploading file with sensorMac:', sensorMac);

                    // Use the correct uploadFile method with correct field names
                    await attachmentApi.uploadFile(
                        fileObj.file,
                        sensorMac,
                        getFileType(fileObj.name)
                    );

                    // Mark as completed
                    setFiles((prev) =>
                        prev.map((f) =>
                            f.id === fileObj.id ? { ...f, status: 'completed' } : f
                        )
                    );

                    successCount++;
                } catch (err) {
                    console.error('Upload error for file:', fileObj.name, err);
                    setFiles((prev) =>
                        prev.map((f) =>
                            f.id === fileObj.id ? { ...f, status: 'failed', error: err.message } : f
                        )
                    );
                    failedCount++;
                }
            }

            // Show summary
            if (successCount > 0) {
                show(
                    `${successCount} file${successCount > 1 ? 's' : ''} uploaded successfully`,
                    'success'
                );
            }
            if (failedCount > 0) {
                show(
                    `${failedCount} file${failedCount > 1 ? 's' : ''} failed to upload`,
                    'error'
                );
            }

            // Clear successful files after a delay
            setTimeout(() => {
                setFiles((prev) =>
                    prev.filter((f) => f.status !== 'completed')
                );
                if (onUploadSuccess) {
                    onUploadSuccess();
                }
            }, 2000);
        } catch (err) {
            console.error('Upload error:', err);
            show('Upload failed. Please try again.', 'error');
        } finally {
            setUploading(false);
        }
    };

    return (
        <div className="w-full">
            {/* Drop Zone */}
            <div
                onDragEnter={handleDrag}
                onDragLeave={handleDrag}
                onDragOver={handleDrag}
                onDrop={handleDrop}
                className={`relative border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition mb-4 ${
                    dragActive
                        ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20'
                        : 'border-gray-300 dark:border-gray-600'
                }`}
                onClick={() => document.getElementById('file-input')?.click()}
            >
                <input
                    type="file"
                    id="file-input"
                    className="hidden"
                    onChange={handleFileInput}
                    accept={allowedExtensions.join(',')}
                    multiple
                    disabled={uploading}
                />

                <div className="flex flex-col items-center justify-center gap-3">
                    <div className="p-3 bg-primary-100 dark:bg-primary-900/30 rounded-lg">
                        <Upload size={24} className="text-primary-600 dark:text-primary-400" />
                    </div>
                    <div>
                        <p className="text-sm font-medium text-gray-900 dark:text-white">
                            Drag and drop your files here, or click to browse
                        </p>
                        <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                            Supported: Configuration files, photos, logs (Max 50 MB)
                        </p>
                    </div>
                </div>
            </div>

            {/* File List */}
            {files.length > 0 && (
                <div className="space-y-2 mb-4">
                    {files.map((fileObj) => (
                        <div
                            key={fileObj.id}
                            className="bg-gray-50 dark:bg-gray-700 rounded-lg p-3 flex items-center justify-between"
                        >
                            <div className="flex-1 min-w-0">
                                <p className="text-sm font-medium text-gray-900 dark:text-white truncate">
                                    {fileObj.name}
                                </p>
                                <p className="text-xs text-gray-600 dark:text-gray-400">
                                    {formatFileSize(fileObj.size)} • {getFileType(fileObj.name)}
                                </p>
                            </div>

                            <div className="flex items-center gap-2 ml-3 flex-shrink-0">
                                {fileObj.status === 'completed' && (
                                    <CheckCircle size={18} className="text-success-500" />
                                )}
                                {fileObj.status === 'uploading' && (
                                    <Loader size={18} className="text-primary-500 animate-spin" />
                                )}
                                {fileObj.status === 'failed' && (
                                    <AlertCircle size={18} className="text-danger-500" />
                                )}

                                {fileObj.status === 'pending' && !uploading && (
                                    <button
                                        type="button"
                                        onClick={() => removeFile(fileObj.id)}
                                        className="p-1 hover:bg-gray-200 dark:hover:bg-gray-600 rounded transition"
                                    >
                                        <X size={16} className="text-gray-600 dark:text-gray-400" />
                                    </button>
                                )}
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Upload Button */}
            {files.some((f) => f.status === 'pending') && (
                <button
                    onClick={uploadFiles}
                    disabled={uploading}
                    className="w-full px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed transition font-medium flex items-center justify-center gap-2"
                >
                    {uploading ? (
                        <>
                            <Loader size={16} className="animate-spin" />
                            Uploading...
                        </>
                    ) : (
                        <>
                            <Upload size={16} />
                            Upload {files.filter((f) => f.status === 'pending').length} File
                            {files.filter((f) => f.status === 'pending').length > 1 ? 's' : ''}
                        </>
                    )}
                </button>
            )}
        </div>
    );
};

export default AttachmentUpload;
