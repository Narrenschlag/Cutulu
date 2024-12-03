shader_type canvas_item;


uniform bool stepify = true;
uniform float value : hint_range(0.0, 1.0) = 1.0;
uniform int count = 1;
uniform vec2 margin;
uniform float shear_angle : hint_range(-1.0, 1.0) = 0.0; // -90 to 90 degrees
uniform sampler2D gradient_x : source_color;
uniform sampler2D gradient_y : source_color;
uniform bool use_value_gradient = false;
uniform bool invert = false;


vec4 get_gradient_color(sampler2D src, float position) {
	position = clamp(position, 0.01, 0.99); // Color at 0.0 and 1.0 get interpolated by both end
    return texture(src, vec2(position, 0.5));
}

float square_shape(vec2 uv, vec2 size) {
	vec2 bl = step((1.0-size)/2.0, uv); // bottom-left
	vec2 tr = step((1.0-size)/2.0, 1.0-uv); // top-right
	return bl.x * bl.y * tr.x * tr.y;
}

vec2 segmented_bar(vec2 uv) {
	float shape = 0.0;
	float bar_count = float(count);
	float bar_size = 1.0/bar_count;
	float separation = margin.x;
	float separation_width = separation * bar_size;
	float bar_value = 0.0;
	for (int i=0;i<count;i++) {
		float index_value = float(i+1) / float(count);
		
		if (index_value > value) {
			// Ceil
			if (value < float(i) * bar_size || value <= 0.0) {
				continue;
			}
		}
		
		vec2 uv4 = uv;
		vec2 size4 = vec2(bar_size, 1.0-margin.y);
		//size4.x = bar_size;
		uv4.x = uv4.x - (bar_size/2.0) + 0.5;
		uv4.x -= bar_size * float(i); // Offset
		size4.x -= separation_width;
		shape += square_shape(uv4, size4);
		bar_value = index_value;
	}
	return vec2(shape, bar_value);
}

float snap(float original, float numerator, float denominator)
{
    return round(original * denominator / numerator) * numerator / denominator;
}

vec3 hsv_to_rgb(vec3 color) {
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(color.xxx + K.xyz) * 6.0 - K.www);
	return color.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), color.y);
}

float round_place(float num,float places) {
	return (round(num*pow(10,places))/pow(10,places));
}

float map_range(float min1, float max1, float min2, float max2, float v) {
	float p = (v - min1) / (max1 - min1);
	return p * (max2 - min2) + min2;
}

void fragment() {
	vec2 uv = UV;
	
	float angle = 1.571 + (shear_angle * PI/4.0);
	float shear_value = (cos(angle)/sin(angle));
	float shear = shear_value * uv.y;
	uv.x = uv.x + shear;
	uv.x -= shear_value/2.0;
	
	uv.x *= 1.0+abs(shear_value);
	uv.x -= abs(shear_value)/2.0;
	
	float bar_count = float(count);
	float bar_size = 1.0/bar_count;
	float separation_width = margin.x * bar_size;
	
	vec2 shape = segmented_bar(uv);
	vec4 gradient_color = vec4(0.0);
	float gradient_x_pos = uv.x;
	if (use_value_gradient) {
		gradient_x_pos = value;
	}
	vec4 gradient_color_x = get_gradient_color(gradient_x, gradient_x_pos);
	if (stepify) {
		float step_size = float(count)/2.0;
		gradient_color_x = get_gradient_color(gradient_x, snap(gradient_x_pos, 1.0, step_size));
	}
	else {
		shape.x *= step(uv.x, value);
	}
	
	float y = map_range(0.5-(1.0-margin.y)/2.0, 0.5+(1.0-margin.y)/2.0, 0.0, 1.0, uv.y);
	if (invert) {
		y = uv.y;
		shape.x = 1.0 - shape.x;
		shape.x *= step(0.0, y);
		shape.x *= step(y, 1.0);
	}
	vec4 gradient_color_y = get_gradient_color(gradient_y, y);
	gradient_color += gradient_color_x;
	gradient_color = mix(gradient_color_x, gradient_color_y, gradient_color_y.a);
	
	
	
	COLOR = vec4(gradient_color.rgb, shape.x);
}