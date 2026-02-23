import { defineConfig } from 'vitepress'

export default defineConfig({
    title: "Unity AI Voice Over",
    description: "A powerful Unity Editor plugin for seamless AI Voice Over integration.",
    base: '/voiceover/',
    head: [
        [
            'script',
            { async: '', src: 'https://www.googletagmanager.com/gtag/js?id=G-8SC6ZEF7VE' }
        ],
        [
            'script',
            {},
            `window.dataLayer = window.dataLayer || [];\nfunction gtag(){dataLayer.push(arguments);}\ngtag('js', new Date());\ngtag('config', 'G-8SC6ZEF7VE');`
        ]
    ],
    appearance: true,
    themeConfig: {
        nav: [
            { text: 'Home', link: '/' },
            { text: 'Guide', link: '/guide/getting-started' },
            { text: 'GitHub', link: 'https://github.com/Yokesh-4040/Unity-VoiceOver-Plugin' }
        ],
        sidebar: [
            {
                text: 'Guide',
                items: [
                    { text: 'Getting Started', link: '/guide/getting-started' },
                    { text: 'Roadmap & Features', link: '/guide/features' },
                    { text: 'API Reference', link: '/guide/api' },
                    { text: 'Changelog', link: '/guide/changelog' }
                ]
            }
        ],
        socialLinks: [],
        footer: {
            message: 'Released under the MIT License.',
            copyright: 'Copyright © 2026-present Yokesh'
        }
    }
})
