"use client";

import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeHighlight from 'rehype-highlight';
import 'highlight.js/styles/github-dark.css';

interface CustomMessageRendererProps {
  content: string;
  role: 'user' | 'assistant' | 'system';
}

/**
 * Custom message renderer component that supports markdown rendering
 * with code syntax highlighting for AI chat messages.
 */
export function CustomMessageRenderer({ content, role }: CustomMessageRendererProps) {
  // For user messages, just display plain text
  if (role === 'user') {
    return (
      <div className="user-message">
        <p className="text-gray-900 dark:text-white">{content}</p>
      </div>
    );
  }

  // For assistant/system messages, render markdown with code highlighting
  return (
    <div className="assistant-message prose dark:prose-invert max-w-none">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        rehypePlugins={[rehypeHighlight]}
        components={{
          // Customize code block rendering
          code({ node, inline, className, children, ...props }: any) {
            const match = /language-(\w+)/.exec(className || '');
            return !inline ? (
              <pre className={className}>
                <code className={className} {...props}>
                  {children}
                </code>
              </pre>
            ) : (
              <code className="bg-gray-100 dark:bg-gray-800 px-1 py-0.5 rounded text-sm" {...props}>
                {children}
              </code>
            );
          },
          // Style links
          a({ node, children, ...props }: any) {
            return (
              <a
                className="text-blue-600 dark:text-blue-400 hover:underline"
                target="_blank"
                rel="noopener noreferrer"
                {...props}
              >
                {children}
              </a>
            );
          },
        }}
      >
        {content}
      </ReactMarkdown>
    </div>
  );
}
