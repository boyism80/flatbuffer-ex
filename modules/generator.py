import jinja2
import os
import copy
import modules.extractor as extractor

def loadTemplateFiles(root):
    loader = jinja2.FileSystemLoader(root)
    env = jinja2.Environment(loader=loader)

    return {os.path.splitext(x)[0]:env.get_template(x) for x in os.listdir(root) if x.endswith('.txt')}

templates = loadTemplateFiles('templates')


def go(root, package, name, params):

    params = copy.deepcopy(params)
    for param in params:
        param['type'] = extractor.py2go(param['type'])

    return templates['go'].render({
        'root': root.replace('\\', '/'),
        'package': package,
        'name': name,
        'params': params
    })

def cs(namespace, name, params):
    params = copy.deepcopy(params)
    for param in params:
        param['type'] = extractor.py2cs(param['type'])

    return templates['cs'].render({
        'namespace': namespace,
        'name': name,
        'params': params
    })
    })