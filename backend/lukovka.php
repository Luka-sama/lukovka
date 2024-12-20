<?php
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);
define('ACCESS_KEY', 'YGDOMvwzrPy87u2dfN0r');

function stop($error) {
	http_response_code(501);
	exit($error);
}

(isset($_GET['key']) && $_GET['key'] == ACCESS_KEY) or stop("wrong key");
$table = (isset($_GET['states']) ? 'states' : 'tasks');

try {
	$pdo = new PDO('pgsql:host=localhost;dbname=lukovka', 'lukovka', 'RDmNOf9SsFFliWaUBDEJ', [
		PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
		PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
	]);
} catch (PDOException $e) {
	stop("Database connection failed: " . $e->getMessage());
}

function convertKeysToSnakeCase($data) {
    $converted = [];
    foreach ($data as $key => $value) {
        $snakeKey = strtolower(preg_replace('/([a-z])([A-Z])/', '$1_$2', $key));
        $converted[$snakeKey] = $value;
    }
    return $converted;
}

function pgArrayParse($literal) {
	if ($literal == '') return;
	preg_match_all('/(?<=^\{|,)(([^,"{]*)|\s*"((?:[^"\\\\]|\\\\(?:.|[0-9]+|x[0-9a-f]+))*)"\s*)(,|(?<!^\{)(?=\}$))/i', $literal, $matches, PREG_SET_ORDER);
	$values = [];
	foreach ($matches as $match) {
		$values[] = $match[3] != '' ? stripcslashes($match[3]) : (strtolower($match[2]) == 'null' ? null : $match[2]);
	}
	return $values;
}

function prepareResult($row) {
	$result = [];
	foreach ($row as $key => $value) {
		if (empty($value) || (isset($_GET['states']) && $key == 'id')) {
			continue;
		}
		
		$camelCaseKey = str_replace('_', '', ucwords($key, '_'));
		if (in_array($key, ['tags', 'selected_filters'])) {
			$result[$camelCaseKey] = pgArrayParse($value);
		} else {
			$result[$camelCaseKey] = $value;
		}
	}
	return $result;
}

$method = $_SERVER['REQUEST_METHOD'];
if ($method == 'GET') {
	$stmt = $pdo->query("SELECT * FROM \"$table\"");
	$results = $stmt->fetchAll();
	$filteredResults = array_map('prepareResult', $results);
	header('Content-Type: application/json');
	echo json_encode($filteredResults, JSON_UNESCAPED_UNICODE);
} elseif (in_array($method, ['POST', 'PUT', 'DELETE'])) {
	$data = json_decode(file_get_contents('php://input'), true);
	(!empty($data)) or stop("Invalid input data");

	if ($method === 'POST') {
		$data = convertKeysToSnakeCase($data);
		$columns = array_keys($data);

		$placeholders = array_map(fn($col) => "$col = :$col", $columns);
		$query = "UPDATE \"$table\" SET " . implode(', ', $placeholders) . " WHERE id = :id";

		$stmt = $pdo->prepare($query);
		$stmt->execute($data);
	} elseif ($method === 'PUT') {
		$data = convertKeysToSnakeCase($data);
		$columns = array_keys($data);

		$query = "INSERT INTO \"$table\" (" . implode(', ', $columns) . ") VALUES (" . implode(', ', array_map(fn($col) => ":$col", $columns)) . ")";

		$stmt = $pdo->prepare($query);
		$stmt->execute($data);
	} elseif ($method === 'DELETE') {
		$column = ($table == 'states' ? 'name' : 'id');
		$query = "DELETE FROM \"$table\" WHERE \"$column\" = :value";
		$stmt = $pdo->prepare($query);
		$stmt->execute(['value' => $data]);
	}
} else {
	stop("Unsupported HTTP method");
}
