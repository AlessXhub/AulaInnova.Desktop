# Aula Innova Desktop Design System

## Direction

Trust-first institutional workspace. The application should feel precise and contemporary without resembling a generic analytics template. Information density is moderate-high, while critical distinctions use copy and state rather than decoration.

Design calibration used during implementation:

- Design variance: 4/10
- Motion intensity: 3/10
- Information density: 6/10

## Brand tokens

| Token | Value | Use |
|---|---:|---|
| Brand navy | `#17233D` | Primary text, structure and contrast |
| Brand violet | `#5967E9` | Navigation, actions and focus |
| Violet dark | `#4653C7` | Hover and strong action state |
| Brand lime | `#D6E85C` | Brand accent and sparing emphasis |
| Brand lavender | `#A3AFF4` | Selection and supporting accents |
| Canvas | `#F6F7FC` | Main application background |
| Surface | `#FFFFFF` | Cards, dialogs and tables |
| Border | `#DDE1EC` | Separation without heavy shadows |

Semantic success, warning, danger and information colors are defined in `Themes/Colors.xaml`. Status colors never replace the written label.

## Typography

Manrope is packaged with the application and used through `AppFontFamily`. Segoe Fluent Icons is used only for compact interface symbols. Page titles, section headings, body copy and captions use a consistent hierarchy from `Themes/Controls.xaml`.

## Composition

- 252 px expanded navigation rail with grouped destinations and an explicit selected state.
- 72 px application bar with page context, demonstration badge and session controls.
- Main pages use 28 px outer padding and a 1500 px practical content ceiling.
- Cards use a 14 px radius, one-pixel border and no decorative gradient.
- Tables support horizontal scrolling when columns exceed the viewport.
- Dialogs maintain keyboard focus cycling and visible close actions.

## Login artwork

The decorative background is a raster asset based on the approved Figma composition. It intentionally contains no text, logo or controls. The supplied white Aula Innova mark, headings, fields, errors and buttons remain real WPF elements for scaling, accessibility and validation.

## Motion

Only page transitions and pressed-button feedback are used. Page entry is approximately 170 ms with a cubic ease-out. Transitions are omitted when Windows client-area animation is disabled.

## Accessibility and privacy

- Visible focus treatment for interactive controls.
- Automation names for important navigation, login and record actions.
- Minimum 34-44 px interactive heights depending on density.
- Text labels accompany colors and icons.
- Fictitious data only.
- No individual visual tracking or unsupported behavioral inference.

