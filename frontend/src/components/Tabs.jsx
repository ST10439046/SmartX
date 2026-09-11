import  { useState, createContext, useContext } from 'react';

const TabsContext = createContext();

export const Tabs = ({ value, onValueChange, children, defaultValue }) => {
  const [internalValue, setInternalValue] = useState(defaultValue || value || '');

  const currentValue = value !== undefined ? value : internalValue;
  const handleChange = onValueChange || setInternalValue;

  return (
    <TabsContext.Provider value={{ value: currentValue, onChange: handleChange }}>
      {children}
    </TabsContext.Provider>
  );
};

export const TabsList = ({ children }) => {
  return (
    <div className="flex border-b border-gray-200 dark:border-gray-700 gap-4 mb-4 overflow-x-auto">
      {children}
    </div>
  );
};

export const TabsTrigger = ({ value, children }) => {
  const { value: currentValue, onChange } = useContext(TabsContext);
  const isActive = currentValue === value;

  return (
    <button
      onClick={() => onChange(value)}
      className={`
        px-4 py-3 font-medium text-sm whitespace-nowrap border-b-2 transition-colors
        ${
          isActive
            ? 'border-primary-600 text-primary-600 dark:text-primary-400'
            : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300'
        }
      `}
    >
      {children}
    </button>
  );
};

export const TabsContent = ({ value, children }) => {
  const { value: currentValue } = useContext(TabsContext);

  if (currentValue !== value) {
    return null;
  }

  return <div className="fade-in">{children}</div>;
};
