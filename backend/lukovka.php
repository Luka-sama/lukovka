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

function prepareData($data) {
	$converted = [];
	foreach ($data as $key => $value) {
		$snakeKey = strtolower(preg_replace('/([a-z])([A-Z])/', '$1_$2', $key));
		if (is_array($value)) {
			$value = '{' . implode(',', array_map(fn($v) => is_string($v) ? '"' . addslashes($v) . '"' : $v, $value)) . '}';
		}
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
		if (in_array($key, ['time_entries'])) {
			$result[$camelCaseKey] = json_decode($value);
		} else if (in_array($key, ['tags', 'selected_filters'])) {
			$result[$camelCaseKey] = pgArrayParse($value);
		} else {
			$result[$camelCaseKey] = $value;
		}
	}
	return $result;
}

function executeQuery($query, $data) {
	global $pdo;
	$stmt = $pdo->prepare($query);
	foreach ($data as $key => $value) {
		$type = match (true) {
			is_int($value) => PDO::PARAM_INT,
			is_bool($value) => PDO::PARAM_BOOL,
			is_null($value) => PDO::PARAM_NULL,
			default => PDO::PARAM_STR,
		};
		$stmt->bindValue(":$key", $value, $type);
	}
	$stmt->execute();
}

function getTableColumns($table) {
	global $pdo;
	$query = "SELECT column_name FROM information_schema.columns WHERE table_name = :table_name AND table_schema = 'public'";
	$stmt = $pdo->prepare($query);
	$stmt->execute(['table_name' => $table]);

	return $stmt->fetchAll(PDO::FETCH_COLUMN);
}

$method = $_SERVER['REQUEST_METHOD'];
if ($method == 'GET') {
	$query = "SELECT * FROM \"$table\"";
	if ($table == 'tasks') {
		$query = "SELECT
    t.*,
    COALESCE(
        JSON_AGG(
            CASE
                WHEN te.end IS NOT NULL THEN
                    JSON_BUILD_OBJECT('start', te.start, 'end', te.end)
                ELSE
                    JSON_BUILD_OBJECT('start', te.start)
            END
        ) FILTER (WHERE te.id IS NOT NULL),
        '[]'
    )::TEXT AS time_entries
FROM
    tasks t
LEFT JOIN
    tasks_time_entries te
ON
    t.id = te.task
GROUP BY
    t.id";
	}
	$stmt = $pdo->query($query);
	$results = $stmt->fetchAll();
	$filteredResults = array_map('prepareResult', $results);
	header('Content-Type: application/json');
	echo json_encode($filteredResults, JSON_UNESCAPED_UNICODE);
} elseif (in_array($method, ['POST', 'PUT', 'DELETE'])) {
	$data = json_decode(file_get_contents('php://input'), true);
	(!empty($data)) or stop("Invalid input data");

	if ($method === 'POST') {
		$data = prepareData($data);
		$dataColumns = array_keys($data);
		$tableColumns = getTableColumns($table);
		$missingColumns = array_diff($tableColumns, $dataColumns, ['id']);
		$dataPlaceholders = array_map(fn($col) => "\"$col\" = :$col", $dataColumns);
		$defaultPlaceholders = array_map(fn($col) => "\"$col\" = DEFAULT", $missingColumns);
		$placeholders = array_merge($dataPlaceholders, $defaultPlaceholders);
		$query = "UPDATE \"$table\" SET " . implode(', ', $placeholders) . " WHERE id = :id";
		executeQuery($query, $data);
	} elseif ($method === 'PUT') {
		$data = prepareData($data);
		$columns = array_keys($data);

		$query = "INSERT INTO \"$table\" (" . implode(', ', array_map(fn($col) => "\"$col\"", $columns)) . ") VALUES (" . implode(', ', array_map(fn($col) => ":$col", $columns)) . ")";
		executeQuery($query, $data);
	} elseif ($method === 'DELETE') {
		$column = ($table == 'states' ? 'name' : 'id');
		$query = "DELETE FROM \"$table\" WHERE \"$column\" = :value";
		executeQuery($query, ['value' => $data]);
	}
} else {
	stop("Unsupported HTTP method");
}
