import { defineConfig } from 'vitepress'

export default defineConfig({
    title: "Unity AI Voice Over",
    description: "A powerful Unity Editor plugin for seamless AI Voice Over integration.",
    base: '/',
    themeConfig: {
        nav: [
            { text: 'Home', link: '/' },
            { text: 'Guide', link: '/guide/getting-started' },
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
        socialLinks: [
            { icon: 'github', link: 'https://github.com/Yokesh-4040/Unity-VoiceOver-Plugin' }
        ],
        footer: {
            message: 'Released under the MIT License.',
            copyright: 'Copyright © 2026-present Yokesh'
        }
    }
})
