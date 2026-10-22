// Rotates the hue of a texture. Used to recolor the loading bar fill (red art -> blue).
// Note: this matrix moves the hue backwards, so 130 turns the crimson fill blue.
shader_type canvas_item;

uniform float hue_degrees = 130.0;

void fragment() {
	vec4 tex = texture(TEXTURE, UV) * COLOR;
	float a = radians(hue_degrees);
	float c = cos(a);
	float s = sin(a);
	vec3 rgb = tex.rgb;
	float r = dot(vec3(0.299 + 0.701 * c + 0.168 * s, 0.587 - 0.587 * c + 0.330 * s, 0.114 - 0.114 * c - 0.497 * s), rgb);
	float g = dot(vec3(0.299 - 0.299 * c - 0.328 * s, 0.587 + 0.413 * c + 0.035 * s, 0.114 - 0.114 * c + 0.292 * s), rgb);
	float b = dot(vec3(0.299 - 0.300 * c + 1.250 * s, 0.587 - 0.588 * c - 1.050 * s, 0.114 + 0.886 * c - 0.203 * s), rgb);
	COLOR = vec4(clamp(vec3(r, g, b), 0.0, 1.0), tex.a);
}
