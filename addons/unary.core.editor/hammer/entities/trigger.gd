@tool
class_name Trigger extends VMFEntityNode

var brush_entity = preload("res://unary.core/sources/level/BrushEntity.cs")

func _entity_setup(_entity: VMFEntity):
	var mesh = get_mesh();

	if !mesh or mesh.get_surface_count() == 0:
		queue_free();
		return;
	
	var group = _entity.data.get("groupname") as String
	$Area3D.add_to_group.call_deferred(group, true)

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
	
	self.set_script(brush_entity)
	call_deferred("_ready")
