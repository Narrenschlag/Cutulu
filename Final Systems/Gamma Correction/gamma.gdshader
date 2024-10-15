shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, filter_nearest_mipmap, repeat_enable;

uniform float gamma: hint_range(0.0, 2.0, 0.05) = 1.0;

void fragment() {
    vec4 color = texture(screen_texture, SCREEN_UV);
    color.rgb = pow(color.rgb, vec3(1.0 / gamma));
    COLOR = color;
}