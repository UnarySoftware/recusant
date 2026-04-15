@tool
class_name BrushNav extends VMFEntityNode

var nav_brush = preload("res://unary.recusant/sources/level/NavBrush.cs")

func _entity_setup(_entity: VMFEntity):
	var mesh = get_mesh();

	if !mesh or mesh.get_surface_count() == 0:
		queue_free();
		return;

	var color = _entity.data.get("rendercolor")
	
	if color is Color:
		color.a = 1.0
		$Area3D/CollisionShape3D.debug_color = color 
	elif color is Vector3:
		$Area3D/CollisionShape3D.debug_color = Color8(color.x, color.y, color.z, 255)
	
	var aabb : AABB = mesh.get_aabb()
	
	var box : BoxShape3D = BoxShape3D.new()
	box.size = aabb.size
	$Area3D/CollisionShape3D.shape = box
	
	set_script.call_deferred(nav_brush)
	set.call_deferred("Type", _entity.data.get("type"))
	set.call_deferred("Flags", _entity.data.get("spawnflags"))
	call_deferred("_ready")
